-- Reference data required by the API (statuses, types, demo game parameters, app balance).
-- Run after schema.sql. Names are in Russian because the admin panel shows them as they are. The values below are demo values for local development only.

INSERT INTO "TransactionStatuses" ("Id", "Name") OVERRIDING SYSTEM VALUE VALUES
    (1, 'В ожидании'),
    (2, 'Одобрено'),
    (3, 'Отклонено');

INSERT INTO "TransactionTypes" ("Id", "Name") OVERRIDING SYSTEM VALUE VALUES
    (1, 'Пополнение'),
    (2, 'Вывод');

INSERT INTO "AppBalance" ("Id", "Amount", "LowerThreshold") OVERRIDING SYSTEM VALUE VALUES
    (1, 100000, 10000);

INSERT INTO "GameParams" (
    "Id", "BaseWinChance", "MinDepositAmount", "MinRoundsCountForWithdrawal",
    "MinWinChance", "MaxWinChance", "ChanceDecPerThrowMin", "ChanceDecPerThrowMax",
    "ChanceDecPerRotation", "MinRotationWinChance", "MinMultiplier", "MaxMultiplier",
    "InitialBalanceLoss", "DeviationChance", "MaxDeviationPercent", "MinGamesForBoost",
    "WinBoostChance", "WinBoostMultiplier", "HardResetChance", "ControlGoal",
    "ReelSpeed", "UprightThresholdForLine"
) OVERRIDING SYSTEM VALUE VALUES (
    1, 0.5, 100, 5,
    0.1, 0.9, 0.05, 0.1,
    0.05, 0.1, 1.5, 2.0,
    0.1, 0.5, 0.5, 10,
    0.1, 2.0, 0.05, 100,
    1.0, 0.5
);

-- Keep identity sequences in step with the explicit ids above.
SELECT setval(pg_get_serial_sequence('"TransactionStatuses"', 'Id'), 3);
SELECT setval(pg_get_serial_sequence('"TransactionTypes"', 'Id'), 2);
SELECT setval(pg_get_serial_sequence('"AppBalance"', 'Id'), 1);
SELECT setval(pg_get_serial_sequence('"GameParams"', 'Id'), 1);
