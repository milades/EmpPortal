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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    IF SCHEMA_ID(N'security') IS NULL EXEC(N'CREATE SCHEMA [security];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    IF SCHEMA_ID(N'portal') IS NULL EXEC(N'CREATE SCHEMA [portal];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [security].[ApplicationSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [LastActivityAtUtc] datetimeoffset NOT NULL,
        [AbsoluteExpiresAtUtc] datetimeoffset NOT NULL,
        [IdleExpiresAtUtc] datetimeoffset NOT NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [RevocationReason] nvarchar(512) NULL,
        CONSTRAINT [PK_ApplicationSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(512) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [DirectoryObjectGuid] uniqueidentifier NOT NULL,
        [Sid] nvarchar(256) NOT NULL,
        [DisplayName] nvarchar(256) NOT NULL,
        [IsDirectoryEnabled] bit NOT NULL,
        [LastDirectoryValidationAtUtc] datetimeoffset NOT NULL,
        [AuthorizationVersion] bigint NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(256) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [portal].[AspNetUserPasskeys] (
        [CredentialId] varbinary(1024) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AspNetUserPasskeys] PRIMARY KEY ([CredentialId]),
        CONSTRAINT [FK_AspNetUserPasskeys_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[UserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE TABLE [identity].[UserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_ApplicationSessions_UserId_RevokedAtUtc] ON [security].[ApplicationSessions] ([UserId], [RevokedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_AspNetUserPasskeys_UserId] ON [portal].[AspNetUserPasskeys] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [identity].[RoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [identity].[Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [identity].[UserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [identity].[UserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [identity].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [identity].[Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_DirectoryObjectGuid] ON [identity].[Users] ([DirectoryObjectGuid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Sid] ON [identity].[Users] ([Sid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [identity].[Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803165913_InitialIdentityAndSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803165913_InitialIdentityAndSessions', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803171657_AddSessionUserForeignKey'
)
BEGIN
    ALTER TABLE [security].[ApplicationSessions] ADD CONSTRAINT [FK_ApplicationSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803171657_AddSessionUserForeignKey'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803171657_AddSessionUserForeignKey', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    CREATE TABLE [audit].[AuditEvents] (
        [Id] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [EventType] nvarchar(120) NOT NULL,
        [Outcome] nvarchar(40) NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [ActorUpn] nvarchar(320) NULL,
        [Subject] nvarchar(500) NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [DetailsJson] nvarchar(4000) NULL,
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    CREATE TABLE [portal].[RuntimeSettings] (
        [Key] nvarchar(200) NOT NULL,
        [Value] nvarchar(2000) NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RuntimeSettings] PRIMARY KEY ([Key]),
        CONSTRAINT [FK_RuntimeSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_EventType_Outcome] ON [audit].[AuditEvents] ([EventType], [Outcome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_OccurredAtUtc] ON [audit].[AuditEvents] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    CREATE INDEX [IX_RuntimeSettings_UpdatedByUserId] ON [portal].[RuntimeSettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803173909_AddRuntimeSettingsAndAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803173909_AddRuntimeSettingsAndAudit', N'10.0.10');
END;

COMMIT;
GO

