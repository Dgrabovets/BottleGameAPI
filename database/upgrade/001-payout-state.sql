-- Upgrades a database created from an earlier database/schema.sql: adds the payout columns of "MoneyTransactions"
-- and turns the sentence that earlier versions wrote into "ProcessingError" into "PayoutState" 1 (outcome unknown).
-- Stop the API before running it and start the new version afterwards: the old version would no longer see
-- that these payouts may exist. Running the script again changes nothing.
--   psql -v ON_ERROR_STOP=1 -d <database> -f database/upgrade/001-payout-state.sql

BEGIN;

ALTER TABLE "MoneyTransactions"
    ADD COLUMN IF NOT EXISTS "PayoutState" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "PayoutRequestedAt" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "PayoutWallet" character varying(100);

-- The time and the wallet of these requests were never stored, so the API refuses to repeat them
UPDATE "MoneyTransactions"
SET "PayoutState" = 1,
    "ProcessingError" = NULL
WHERE "ProcessingError" = 'Payout outcome unknown: wait for the YooKassa notification or approve again to repeat the request with the same idempotence key';

COMMIT;
