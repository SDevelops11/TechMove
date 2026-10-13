IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'glms') IS NULL EXEC(N'CREATE SCHEMA [glms];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE TABLE [glms].[Clients] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [ContactPerson] nvarchar(100) NOT NULL,
        [ContactEmail] nvarchar(150) NOT NULL,
        [ContactPhone] nvarchar(30) NOT NULL,
        [Region] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE TABLE [glms].[Contracts] (
        [Id] int NOT NULL IDENTITY,
        [ContractNumber] nvarchar(50) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [ClientId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [ServiceLevel] nvarchar(max) NOT NULL,
        [ContractType] nvarchar(50) NOT NULL,
        [EstimatedValueUsd] decimal(18,2) NOT NULL,
        [EstimatedValueZar] decimal(18,2) NOT NULL,
        [AgreementFileName] nvarchar(255) NULL,
        [AgreementFilePath] nvarchar(500) NULL,
        [AgreementUploadedAt] datetime2 NULL,
        [AgreementFileSizeBytes] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contracts_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [glms].[Clients] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE TABLE [glms].[ContractStatusLogs] (
        [Id] int NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [OldStatus] nvarchar(50) NOT NULL,
        [NewStatus] nvarchar(50) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ActionTaken] nvarchar(500) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_ContractStatusLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractStatusLogs_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [glms].[Contracts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE TABLE [glms].[ServiceRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestNumber] nvarchar(50) NOT NULL,
        [ContractId] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Origin] nvarchar(100) NOT NULL,
        [Destination] nvarchar(100) NOT NULL,
        [OriginalCurrency] nvarchar(10) NOT NULL,
        [OriginalCostUsd] decimal(18,2) NOT NULL,
        [ExchangeRateUsed] decimal(18,6) NOT NULL,
        [Cost] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceRequests_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [glms].[Contracts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContactEmail', N'ContactPerson', N'ContactPhone', N'CreatedAt', N'Name', N'Region') AND [object_id] = OBJECT_ID(N'[glms].[Clients]'))
        SET IDENTITY_INSERT [glms].[Clients] ON;
    EXEC(N'INSERT INTO [glms].[Clients] ([Id], [ContactEmail], [ContactPerson], [ContactPhone], [CreatedAt], [Name], [Region])
    VALUES (1, N''johan.vandermerwe@maersk-sa.com'', N''Johan Van Der Merwe'', N''+27 31 555 0192'', ''2026-01-15T00:00:00.0000000Z'', N''Maersk Southern Africa'', N''Southern Africa (Durban Hub)''),
    (2, N''nolwazi.khumalo@transnet.co.za'', N''Nolwazi Khumalo'', N''+27 11 888 4321'', ''2026-01-15T00:00:00.0000000Z'', N''Transnet Freight Rail'', N''Gauteng Corridor''),
    (3, N''elena.rostova@dhl-global.com'', N''Elena Rostova'', N''+27 21 444 8765'', ''2026-01-15T00:00:00.0000000Z'', N''DHL Global Forwarding'', N''Western Cape Maritime''),
    (4, N''marcus.weber@msc-logistics.com'', N''Marcus Weber'', N''+41 22 703 8888'', ''2026-01-15T00:00:00.0000000Z'', N''Mediterranean Shipping Co.'', N''International (Europe/Geneva)'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContactEmail', N'ContactPerson', N'ContactPhone', N'CreatedAt', N'Name', N'Region') AND [object_id] = OBJECT_ID(N'[glms].[Clients]'))
        SET IDENTITY_INSERT [glms].[Clients] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AgreementFileName', N'AgreementFilePath', N'AgreementFileSizeBytes', N'AgreementUploadedAt', N'ClientId', N'ContractNumber', N'ContractType', N'CreatedAt', N'EndDate', N'EstimatedValueUsd', N'EstimatedValueZar', N'ServiceLevel', N'StartDate', N'Status', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[glms].[Contracts]'))
        SET IDENTITY_INSERT [glms].[Contracts] ON;
    EXEC(N'INSERT INTO [glms].[Contracts] ([Id], [AgreementFileName], [AgreementFilePath], [AgreementFileSizeBytes], [AgreementUploadedAt], [ClientId], [ContractNumber], [ContractType], [CreatedAt], [EndDate], [EstimatedValueUsd], [EstimatedValueZar], [ServiceLevel], [StartDate], [Status], [Title], [UpdatedAt])
    VALUES (1, N''CON-2026-0001_Signed_SLA.pdf'', N''/uploads/contracts/Sample_Signed_Agreement.pdf'', CAST(845 AS bigint), ''2026-01-15T00:00:00.0000000Z'', 1, N''CON-2026-0001'', N''Premium'', ''2026-01-15T00:00:00.0000000Z'', ''2026-12-31T00:00:00.0000000Z'', 250000.0, 4625000.0, N''Premium'', ''2026-01-01T00:00:00.0000000Z'', N''Active'', N''Annual Durban-Joburg Intermodal Freight SLA'', ''2026-01-15T00:00:00.0000000Z''),
    (2, NULL, NULL, NULL, NULL, 2, N''CON-2026-0002'', N''Standard'', ''2026-01-15T00:00:00.0000000Z'', ''2026-08-31T00:00:00.0000000Z'', 120000.0, 2220000.0, N''Standard'', ''2026-02-01T00:00:00.0000000Z'', N''Active'', N''Regional Bulk Cargo Haulage Agreement'', ''2026-01-15T00:00:00.0000000Z''),
    (3, NULL, NULL, NULL, NULL, 3, N''CON-2026-0003'', N''Express'', ''2026-01-15T00:00:00.0000000Z'', ''2027-02-28T00:00:00.0000000Z'', 95000.0, 1757500.0, N''Express'', ''2026-03-01T00:00:00.0000000Z'', N''OnHold'', N''Cold Chain Pharmaceutical Distribution SLA'', ''2026-01-15T00:00:00.0000000Z''),
    (4, NULL, NULL, NULL, NULL, 4, N''CON-2026-0004'', N''International'', ''2025-01-01T00:00:00.0000000Z'', ''2025-12-31T00:00:00.0000000Z'', 500000.0, 9250000.0, N''International'', ''2025-01-01T00:00:00.0000000Z'', N''Expired'', N''Cross-Border Maritime Container Clearance'', ''2025-12-31T00:00:00.0000000Z''),
    (5, NULL, NULL, NULL, NULL, 3, N''CON-2026-0005'', N''Express'', ''2026-01-15T00:00:00.0000000Z'', ''2026-11-30T00:00:00.0000000Z'', 80000.0, 1480000.0, N''Express'', ''2026-06-01T00:00:00.0000000Z'', N''Draft'', N''Special Cargo Air Charter Framework'', ''2026-01-15T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AgreementFileName', N'AgreementFilePath', N'AgreementFileSizeBytes', N'AgreementUploadedAt', N'ClientId', N'ContractNumber', N'ContractType', N'CreatedAt', N'EndDate', N'EstimatedValueUsd', N'EstimatedValueZar', N'ServiceLevel', N'StartDate', N'Status', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[glms].[Contracts]'))
        SET IDENTITY_INSERT [glms].[Contracts] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTaken', N'ContractId', N'Department', N'NewStatus', N'OldStatus', N'Timestamp') AND [object_id] = OBJECT_ID(N'[glms].[ContractStatusLogs]'))
        SET IDENTITY_INSERT [glms].[ContractStatusLogs] ON;
    EXEC(N'INSERT INTO [glms].[ContractStatusLogs] ([Id], [ActionTaken], [ContractId], [Department], [NewStatus], [OldStatus], [Timestamp])
    VALUES (1, N''Allocated primary fleet transport corridors and assigned senior dispatch controllers.'', 1, N''Operations Department'', N''Active'', N''Draft'', ''2026-01-15T00:00:00.0000000Z''),
    (2, N''Opened line of credit, configured billing ledger and currency exchange thresholds.'', 1, N''Finance Department'', N''Active'', N''Draft'', ''2026-01-15T00:00:00.0000000Z''),
    (3, N''Dispatched official activation confirmation and portal credentials to client contact.'', 1, N''Client Services'', N''Active'', N''Draft'', ''2026-01-15T00:00:00.0000000Z''),
    (4, N''Verified legal indemnities, international customs bonds, and SLA adherence criteria.'', 1, N''Compliance Department'', N''Active'', N''Draft'', ''2026-01-15T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActionTaken', N'ContractId', N'Department', N'NewStatus', N'OldStatus', N'Timestamp') AND [object_id] = OBJECT_ID(N'[glms].[ContractStatusLogs]'))
        SET IDENTITY_INSERT [glms].[ContractStatusLogs] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContractId', N'Cost', N'CreatedDate', N'Description', N'Destination', N'ExchangeRateUsed', N'Origin', N'OriginalCostUsd', N'OriginalCurrency', N'RequestNumber', N'Status') AND [object_id] = OBJECT_ID(N'[glms].[ServiceRequests]'))
        SET IDENTITY_INSERT [glms].[ServiceRequests] ON;
    EXEC(N'INSERT INTO [glms].[ServiceRequests] ([Id], [ContractId], [Cost], [CreatedDate], [Description], [Destination], [ExchangeRateUsed], [Origin], [OriginalCostUsd], [OriginalCurrency], [RequestNumber], [Status])
    VALUES (1, 1, 64750.0, ''2026-02-10T08:30:00.0000000Z'', N''Express Container Haulage (40ft Refrigerated - Durban to City Deep)'', N''City Deep Container Depot, JHB'', 18.5, N''Port of Durban Container Terminal'', 3500.0, N''USD'', N''SR-2026-0001'', N''InProgress''),
    (2, 1, 22200.0, ''2026-02-15T11:00:00.0000000Z'', N''Priority Customs Clearance and Port Transfer'', N''Bayhead Logistics Yard'', 18.5, N''Durban Point Pier 2'', 1200.0, N''USD'', N''SR-2026-0002'', N''Completed''),
    (3, 2, 107300.0, ''2026-02-20T14:15:00.0000000Z'', N''Bulk Iron Ore Rail Freight Escort & Inspection'', N''Saldanha Bulk Terminal'', 18.5, N''Sishen Mining Complex'', 5800.0, N''USD'', N''SR-2026-0003'', N''Pending'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ContractId', N'Cost', N'CreatedDate', N'Description', N'Destination', N'ExchangeRateUsed', N'Origin', N'OriginalCostUsd', N'OriginalCurrency', N'RequestNumber', N'Status') AND [object_id] = OBJECT_ID(N'[glms].[ServiceRequests]'))
        SET IDENTITY_INSERT [glms].[ServiceRequests] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Clients_Name] ON [glms].[Clients] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Contracts_ClientId] ON [glms].[Contracts] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contracts_ContractNumber] ON [glms].[Contracts] ([ContractNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Contracts_EndDate] ON [glms].[Contracts] ([EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Contracts_StartDate] ON [glms].[Contracts] ([StartDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Contracts_Status] ON [glms].[Contracts] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContractStatusLogs_ContractId] ON [glms].[ContractStatusLogs] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceRequests_ContractId] ON [glms].[ServiceRequests] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceRequests_RequestNumber] ON [glms].[ServiceRequests] ([RequestNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013174956_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013174956_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

