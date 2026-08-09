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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    IF SCHEMA_ID(N'forms') IS NULL EXEC(N'CREATE SCHEMA [forms];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE TABLE [forms].[FormAccessRules] (
        [Id] uniqueidentifier NOT NULL,
        [FormId] uniqueidentifier NOT NULL,
        [SubjectType] int NOT NULL,
        [SubjectKey] nvarchar(320) NOT NULL,
        [Rights] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_FormAccessRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FormAccessRules_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE TABLE [forms].[FormAnswerIndexes] (
        [Id] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [FieldId] uniqueidentifier NOT NULL,
        [FieldName] nvarchar(100) NOT NULL,
        [FieldType] nvarchar(40) NOT NULL,
        [Sequence] int NOT NULL,
        [StringValue] nvarchar(700) NULL,
        [DecimalValue] decimal(38,10) NULL,
        [DateTimeValue] datetimeoffset NULL,
        [BooleanValue] bit NULL,
        CONSTRAINT [PK_FormAnswerIndexes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE TABLE [forms].[Forms] (
        [Id] uniqueidentifier NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Status] int NOT NULL,
        [CurrentPublishedVersionId] uniqueidentifier NULL,
        [OpensAtUtc] datetimeoffset NULL,
        [ClosesAtUtc] datetimeoffset NULL,
        [AllowDrafts] bit NOT NULL,
        [AllowEditAfterSubmit] bit NOT NULL,
        [MaxSubmissionsPerUser] int NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Forms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Forms_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Forms_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE TABLE [forms].[FormVersions] (
        [Id] uniqueidentifier NOT NULL,
        [FormId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Status] int NOT NULL,
        [DefinitionJson] nvarchar(max) NOT NULL,
        [SchemaHash] char(64) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        [PublishedAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FormVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FormVersions_DefinitionJson_IsJson] CHECK (ISJSON([DefinitionJson]) = 1),
        CONSTRAINT [FK_FormVersions_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [forms].[Forms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FormVersions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FormVersions_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE TABLE [forms].[FormSubmissions] (
        [Id] uniqueidentifier NOT NULL,
        [FormId] uniqueidentifier NOT NULL,
        [FormVersionId] uniqueidentifier NOT NULL,
        [SubmittedByUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [DataJson] nvarchar(max) NOT NULL,
        [TrackingCode] nvarchar(40) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        [SubmittedAtUtc] datetimeoffset NULL,
        [WithdrawnAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FormSubmissions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FormSubmissions_DataJson_IsJson] CHECK (ISJSON([DataJson]) = 1),
        CONSTRAINT [FK_FormSubmissions_FormVersions_FormVersionId] FOREIGN KEY ([FormVersionId]) REFERENCES [forms].[FormVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FormSubmissions_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [forms].[Forms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FormSubmissions_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormAccessRules_CreatedByUserId] ON [forms].[FormAccessRules] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormAccessRules_FormId_SubjectType_SubjectKey] ON [forms].[FormAccessRules] ([FormId], [SubjectType], [SubjectKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormAnswerIndexes_FieldId_DateTimeValue] ON [forms].[FormAnswerIndexes] ([FieldId], [DateTimeValue]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormAnswerIndexes_FieldId_DecimalValue] ON [forms].[FormAnswerIndexes] ([FieldId], [DecimalValue]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormAnswerIndexes_FieldId_StringValue] ON [forms].[FormAnswerIndexes] ([FieldId], [StringValue]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormAnswerIndexes_SubmissionId_FieldId_Sequence] ON [forms].[FormAnswerIndexes] ([SubmissionId], [FieldId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_Forms_CreatedByUserId] ON [forms].[Forms] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_Forms_CurrentPublishedVersionId] ON [forms].[Forms] ([CurrentPublishedVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Forms_Slug] ON [forms].[Forms] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_Forms_Status_OpensAtUtc_ClosesAtUtc] ON [forms].[Forms] ([Status], [OpensAtUtc], [ClosesAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_Forms_UpdatedByUserId] ON [forms].[Forms] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormSubmissions_FormId_Status_SubmittedAtUtc] ON [forms].[FormSubmissions] ([FormId], [Status], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormSubmissions_FormVersionId] ON [forms].[FormSubmissions] ([FormVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FormSubmissions_SubmittedByUserId_FormId] ON [forms].[FormSubmissions] ([SubmittedByUserId], [FormId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormSubmissions_SubmittedByUserId_FormId_Status] ON [forms].[FormSubmissions] ([SubmittedByUserId], [FormId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormSubmissions_TrackingCode] ON [forms].[FormSubmissions] ([TrackingCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormVersions_CreatedByUserId] ON [forms].[FormVersions] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormVersions_FormId_Status] ON [forms].[FormVersions] ([FormId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormVersions_FormId_VersionNumber] ON [forms].[FormVersions] ([FormId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    CREATE INDEX [IX_FormVersions_UpdatedByUserId] ON [forms].[FormVersions] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    ALTER TABLE [forms].[FormAccessRules] ADD CONSTRAINT [FK_FormAccessRules_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [forms].[Forms] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    ALTER TABLE [forms].[FormAnswerIndexes] ADD CONSTRAINT [FK_FormAnswerIndexes_FormSubmissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [forms].[FormSubmissions] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    ALTER TABLE [forms].[Forms] ADD CONSTRAINT [FK_Forms_FormVersions_CurrentPublishedVersionId] FOREIGN KEY ([CurrentPublishedVersionId]) REFERENCES [forms].[FormVersions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804090939_AddDynamicFormsPhaseTwo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804090939_AddDynamicFormsPhaseTwo', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    IF SCHEMA_ID(N'hr') IS NULL EXEC(N'CREATE SCHEMA [hr];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    ALTER TABLE [identity].[Users] ADD [PersonnelCode] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE TABLE [hr].[PayslipPeriodSettings] (
        [Id] uniqueidentifier NOT NULL,
        [PersianYear] int NOT NULL,
        [PersianMonth] int NOT NULL,
        [IsVisibleToEmployees] bit NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PayslipPeriodSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayslipPeriodSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE TABLE [security].[PortalAccessGrants] (
        [Id] uniqueidentifier NOT NULL,
        [ResourceKey] nvarchar(120) NOT NULL,
        [SubjectType] int NOT NULL,
        [SubjectKey] nvarchar(320) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PortalAccessGrants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PortalAccessGrants_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_PersonnelCode] ON [identity].[Users] ([PersonnelCode]) WHERE [PersonnelCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayslipPeriodSettings_PersianYear_PersianMonth] ON [hr].[PayslipPeriodSettings] ([PersianYear], [PersianMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE INDEX [IX_PayslipPeriodSettings_UpdatedByUserId] ON [hr].[PayslipPeriodSettings] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE INDEX [IX_PortalAccessGrants_CreatedByUserId] ON [security].[PortalAccessGrants] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE INDEX [IX_PortalAccessGrants_ResourceKey] ON [security].[PortalAccessGrants] ([ResourceKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PortalAccessGrants_ResourceKey_SubjectType_SubjectKey] ON [security].[PortalAccessGrants] ([ResourceKey], [SubjectType], [SubjectKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809154239_AddPortalAccessPayslipAndPersonnelCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809154239_AddPortalAccessPayslipAndPersonnelCode', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    ALTER TABLE [identity].[Roles] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE TABLE [hr].[CharityPledges] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [Mode] int NOT NULL,
        [StartPersianYear] int NOT NULL,
        [StartPersianMonth] int NOT NULL,
        [EndPersianYear] int NULL,
        [EndPersianMonth] int NULL,
        [Note] nvarchar(500) NULL,
        [IsConfirmed] bit NOT NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_CharityPledges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CharityPledges_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CharityPledges_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CharityPledges_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE TABLE [hr].[PersonnelProfiles] (
        [UserId] uniqueidentifier NOT NULL,
        [InternalPhone] nvarchar(32) NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PersonnelProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_PersonnelProfiles_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE TABLE [hr].[PersonnelVehicles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PlateNumber] nvarchar(32) NOT NULL,
        [VehicleType] nvarchar(80) NOT NULL,
        [Trim] nvarchar(80) NULL,
        [Model] nvarchar(80) NULL,
        [Color] nvarchar(40) NULL,
        [Notes] nvarchar(500) NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PersonnelVehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PersonnelVehicles_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelVehicles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_CharityPledges_CreatedByUserId] ON [hr].[CharityPledges] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_CharityPledges_UpdatedByUserId] ON [hr].[CharityPledges] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_CharityPledges_UserId_CreatedAtUtc] ON [hr].[CharityPledges] ([UserId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_PersonnelProfiles_UpdatedByUserId] ON [hr].[PersonnelProfiles] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_PersonnelVehicles_UpdatedByUserId] ON [hr].[PersonnelVehicles] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    CREATE INDEX [IX_PersonnelVehicles_UserId] ON [hr].[PersonnelVehicles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809173807_AddPersonnelCharityDynamicRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809173807_AddPersonnelCharityDynamicRoles', N'10.0.10');
END;

COMMIT;
GO

