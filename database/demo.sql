-- Demo data for local development: a staff account, players, rounds, transactions and a referral.
-- Not part of the schema or the reference data. Run after schema.sql and seed.sql.
--
-- Staff sign-in for the admin panel:  demo-admin@example.com / demo-admin-password
-- (the hash below matches the placeholder Security__PasswordHashSecretKey in docker-compose.yml)

INSERT INTO "AdminCreds" ("Login", "PasswordHash", "Salt", "CreatedAt", "Role")
VALUES ('demo-admin@example.com', 'aDOqHYWqFoPTqlnxZzyZXNWpWQIGYvq7bgpUqJ90KHk=', 'ZGVtby1zYWx0LTEyMzQ1Ng==', now(), 1);

INSERT INTO "Players" ("TelegramId", "Name", "AvatarUrl", "RegisteredInAppAt")
SELECT 5000 + g, 'Demo player ' || g, '', now() - ((g * 3) || ' days')::interval
FROM generate_series(1, 8) g;

INSERT INTO "PlayersBalance" ("PlayerId", "Amount")
SELECT "Id", 400 + "Id" * 150 FROM "Players";

INSERT INTO "PlayersOptions" ("PlayerId", "WinChance", "Wallet", "Currency", "PayoutMethod", "PaymentMethod", "PaymentDetailType")
SELECT "Id", 0.5, '79001234567:100000000111', 'rub', 'sbp', 'yookassa', 'card' FROM "Players";

INSERT INTO "PlayersPlayedRoundsStatisticsForWithdrawals" ("PlayerId", "PlayedRoundsCount")
SELECT "Id", 10 FROM "Players";

-- Player 1 invited player 2.
INSERT INTO "InvitedPlayers" ("PlayerIdInvited", "PlayerIdInvitedBy", "InvitedAt")
SELECT 2, 1, now() - interval '5 days';

-- Rounds spread over the last 14 days.
INSERT INTO "GameRounds" ("PlayerId", "Bet", "IsWin", "FinishedAt")
SELECT p."Id", 50 + (g % 4) * 25, (g + p."Id") % 3 = 0, now() - (g || ' hours')::interval * 7
FROM "Players" p, generate_series(1, 12) g;

-- Approved deposits over the last 14 days (transaction type 1, status 2).
INSERT INTO "MoneyTransactions" ("PlayerId", "StatusId", "TransactionTypeId", "Amount", "IsProcessingSuccess", "CreatedAt", "ProcessedAt")
SELECT p."Id", 2, 1, 500 + (g % 3) * 250, true, now() - ((g * 2) || ' days')::interval, now() - ((g * 2) || ' days')::interval
FROM "Players" p, generate_series(1, 5) g
WHERE g <= 1 + p."Id" % 5;

-- Withdrawals: two waiting, one approved, one rejected (type 2; statuses 1, 2, 3).
INSERT INTO "MoneyTransactions" ("PlayerId", "StatusId", "TransactionTypeId", "Amount", "IsProcessingSuccess", "CreatedAt", "ProcessedAt")
VALUES
    (1, 1, 2, 300, NULL, now() - interval '3 hours', NULL),
    (3, 1, 2, 450, NULL, now() - interval '1 hour', NULL),
    (4, 2, 2, 200, true, now() - interval '2 days', now() - interval '2 days'),
    (5, 3, 2, 150, false, now() - interval '4 days', now() - interval '4 days');
