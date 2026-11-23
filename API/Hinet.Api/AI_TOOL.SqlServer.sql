USE [master];
GO
IF DB_ID(N'AI_TOOL') IS NULL CREATE DATABASE [AI_TOOL];
GO
USE [AI_TOOL];
GO
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

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [ApiPermissions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [RoleId] uniqueidentifier NULL,
        [Path] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_ApiPermissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Audit] (
        [Id] uniqueidentifier NOT NULL,
        [SessionID] nvarchar(max) NOT NULL,
        [AuditID] uniqueidentifier NOT NULL,
        [IPAddress] nvarchar(max) NOT NULL,
        [UserName] nvarchar(128) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [URLAccessed] nvarchar(max) NOT NULL,
        [TimeAccessed] datetime2 NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Audit] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Department] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [ShortName] nvarchar(max) NULL,
        [Code] nvarchar(250) NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [Priority] bigint NULL,
        [Level] int NOT NULL,
        [Loai] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [DiaDanh] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Department] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [DM_DuLieuDanhMuc] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NULL,
        [Name] nvarchar(250) NOT NULL,
        [Code] nvarchar(250) NOT NULL,
        [Note] nvarchar(max) NULL,
        [Priority] int NULL,
        [DonViId] uniqueidentifier NULL,
        [DuongDanFile] nvarchar(max) NULL,
        [NoiDung] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_DM_DuLieuDanhMuc] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [DM_NhomDanhMuc] (
        [Id] uniqueidentifier NOT NULL,
        [GroupName] nvarchar(150) NOT NULL,
        [GroupCode] nvarchar(150) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_DM_NhomDanhMuc] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Huyen] (
        [Id] uniqueidentifier NOT NULL,
        [TenHuyen] nvarchar(max) NOT NULL,
        [Ma] nvarchar(max) NOT NULL,
        [MaTinh] nvarchar(max) NOT NULL,
        [LoaiHuyen] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Huyen] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Module] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(250) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Order] int NOT NULL,
        [IsShow] bit NOT NULL,
        [Icon] nvarchar(max) NULL,
        [ClassCss] nvarchar(max) NULL,
        [StyleCss] nvarchar(max) NULL,
        [Link] nvarchar(max) NULL,
        [AllowFilterScope] bit NULL,
        [IsMobile] bit NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Module] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Notification] (
        [Id] uniqueidentifier NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Link] nvarchar(max) NOT NULL,
        [FromUser] uniqueidentifier NULL,
        [ToUser] uniqueidentifier NULL,
        [IsRead] bit NOT NULL,
        [Type] nvarchar(250) NOT NULL,
        [SendToFrontEndUser] bit NULL,
        [ItemId] uniqueidentifier NULL,
        [ItemName] nvarchar(128) NOT NULL,
        [ItemType] uniqueidentifier NULL,
        [IsDisplay] bit NULL,
        [DonViId] uniqueidentifier NULL,
        [Email] nvarchar(max) NULL,
        [LoaiThongBao] nvarchar(max) NULL,
        [ProductId] nvarchar(max) NULL,
        [ProductName] nvarchar(max) NULL,
        [TieuDe] nvarchar(max) NOT NULL,
        [NoiDung] nvarchar(max) NOT NULL,
        [NguoiTao] uniqueidentifier NULL,
        [FileDinhKem] nvarchar(max) NULL,
        [IsXuatBan] bit NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Notification] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Operation] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Url] nvarchar(250) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Css] nvarchar(max) NULL,
        [IsShow] bit NOT NULL,
        [Order] int NOT NULL,
        [Icon] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Operation] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Role] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Role] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [RoleOperation] (
        [Id] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [OperationId] uniqueidentifier NOT NULL,
        [IsAccess] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_RoleOperation] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [TaiLieuDinhKem] (
        [Id] uniqueidentifier NOT NULL,
        [KichThuoc] bigint NULL,
        [TenTaiLieu] nvarchar(500) NOT NULL,
        [LoaiTaiLieu] nvarchar(250) NULL,
        [ItemId] uniqueidentifier NULL,
        [DuongDanFile] nvarchar(max) NOT NULL,
        [DuongDanFilePDF] nvarchar(max) NOT NULL,
        [Extension] nvarchar(250) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [TenTaiLieuText] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_TaiLieuDinhKem] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [Tinh] (
        [Id] uniqueidentifier NOT NULL,
        [TenTinh] nvarchar(max) NOT NULL,
        [STT] int NULL,
        [MaTinh] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_Tinh] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [UserRole] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [KhoiCode] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_UserRole] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NULL,
        [Gender] int NOT NULL,
        [Picture] nvarchar(max) NULL,
        [Type] nvarchar(max) NULL,
        [DonViId] uniqueidentifier NULL,
        [NgaySinh] datetime2 NULL,
        [DiaChi] nvarchar(max) NULL,
        [IsSSO] bit NULL,
        [CanBoId] uniqueidentifier NULL,
        [GroupRole] nvarchar(max) NULL,
        [CCCD] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedId] uniqueidentifier NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Department_DonViId] FOREIGN KEY ([DonViId]) REFERENCES [Department] ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'CCCD', N'CanBoId', N'ConcurrencyStamp', N'CreatedBy', N'CreatedDate', N'CreatedId', N'DeletedDate', N'DeletedId', N'DiaChi', N'DonViId', N'Email', N'EmailConfirmed', N'Gender', N'GroupRole', N'IsDeleted', N'IsSSO', N'LockoutEnabled', N'LockoutEnd', N'Name', N'NgaySinh', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'Picture', N'SecurityStamp', N'TwoFactorEnabled', N'Type', N'UpdatedBy', N'UpdatedDate', N'UpdatedId', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] ON;
    EXEC(N'INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [CCCD], [CanBoId], [ConcurrencyStamp], [CreatedBy], [CreatedDate], [CreatedId], [DeletedDate], [DeletedId], [DiaChi], [DonViId], [Email], [EmailConfirmed], [Gender], [GroupRole], [IsDeleted], [IsSSO], [LockoutEnabled], [LockoutEnd], [Name], [NgaySinh], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [Picture], [SecurityStamp], [TwoFactorEnabled], [Type], [UpdatedBy], [UpdatedDate], [UpdatedId], [UserName])
    VALUES (''11111111-1111-1111-1111-111111111111'', 0, NULL, NULL, N''8987c9fc-caf7-4b7c-b3f0-89689dc94106'', NULL, ''0001-01-01T00:00:00.0000000'', NULL, NULL, NULL, NULL, NULL, N''admin'', CAST(1 AS bit), 0, NULL, CAST(0 AS bit), NULL, CAST(0 AS bit), NULL, NULL, NULL, N''ADMIN'', N''ADMIN'', N''AQAAAAIAAYagAAAAEIEmX5aWoTyHzbhuGkjp2g+jgcKJfz1DB6Fq+bxQ6+GPUL2KQlOPmeB4WJlFmSq38A=='', NULL, CAST(0 AS bit), NULL, N''31af2931-6caa-4b92-84a5-5546cbd0a685'', CAST(0 AS bit), N''Admin'', NULL, ''0001-01-01T00:00:00.0000000'', NULL, N''admin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'CCCD', N'CanBoId', N'ConcurrencyStamp', N'CreatedBy', N'CreatedDate', N'CreatedId', N'DeletedDate', N'DeletedId', N'DiaChi', N'DonViId', N'Email', N'EmailConfirmed', N'Gender', N'GroupRole', N'IsDeleted', N'IsSSO', N'LockoutEnabled', N'LockoutEnd', N'Name', N'NgaySinh', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'Picture', N'SecurityStamp', N'TwoFactorEnabled', N'Type', N'UpdatedBy', N'UpdatedDate', N'UpdatedId', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] OFF;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    CREATE INDEX [IX_AspNetUsers_DonViId] ON [AspNetUsers] ([DonViId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041240_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519041240_InitialCreate', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041320_SeedAdminMenu')
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''01f581f4-9ad0-47d3-a36d-44e406a6d7b2'', [PasswordHash] = N''AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg=='', [SecurityStamp] = N''ecd6847d-c789-4e75-be8e-a8b342847a01''
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041320_SeedAdminMenu')
BEGIN

    -- ============ ROLE: Admin ============
    INSERT INTO [Role] ([Id], [Name], [Code], [IsActive], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier), N'Quản trị viên', N'Admin', 1, SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE [Id] = CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier));

    -- ============ USERROLE: admin -> Admin ============
    INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'11111111-1111-1111-1111-111111111111' AS uniqueidentifier), CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier),
           CAST(N'00000000-0000-0000-0000-000000000000' AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (
        SELECT 1 FROM [UserRole]
        WHERE [UserId] = CAST(N'11111111-1111-1111-1111-111111111111' AS uniqueidentifier) AND [RoleId] = CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier)
    );

    -- ============ MODULES ============
    INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), v.code, v.name, v.ord, 1, v.icon, v.icon, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'30000000-0000-0000-0000-000000000001', N'PHANQUYEN', N'Phân quyền hệ thống', 1, N'<SafetyOutlined />'),
        (N'30000000-0000-0000-0000-000000000002',   N'DANHMUC',   N'Danh mục dùng chung', 2, N'<BookOutlined />'),
        (N'30000000-0000-0000-0000-000000000003', N'HANHCHINH', N'Đơn vị hành chính',   3, N'<ApartmentOutlined />'),
        (N'30000000-0000-0000-0000-000000000004',    N'CANHAN',    N'Cá nhân',             4, N'<UserOutlined />')
    ) AS v(id, code, name, ord, icon)
    WHERE NOT EXISTS (SELECT 1 FROM [Module] m WHERE m.[Id] = CAST(v.id AS uniqueidentifier));

    -- ============ OPERATIONS ============
    INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), CAST(v.mid AS uniqueidentifier), v.name, v.url, v.code, v.ord, 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'40000000-0000-0000-0000-000000000001', N'30000000-0000-0000-0000-000000000001', N'Người dùng',        N'/QLNguoiDung',  N'NGUOIDUNG', 1),
        (N'40000000-0000-0000-0000-000000000002',    N'30000000-0000-0000-0000-000000000001', N'Vai trò',           N'/QLRole',       N'VAITRO',    2),
        (N'40000000-0000-0000-0000-000000000004',    N'30000000-0000-0000-0000-000000000001', N'Module',            N'/QLModule',     N'MODULE',    3),
        (N'40000000-0000-0000-0000-000000000005', N'30000000-0000-0000-0000-000000000001', N'Operation',         N'/QLOperation',  N'OPERATION', 4),
        (N'40000000-0000-0000-0000-000000000006',    N'30000000-0000-0000-0000-000000000002',   N'Nhóm danh mục',     N'/NhomDanhMuc',  N'NHOMDM',    1),
        (N'40000000-0000-0000-0000-000000000008',      N'30000000-0000-0000-0000-000000000003', N'Tỉnh / Thành phố',  N'/tinh',         N'TINH',      1),
        (N'40000000-0000-0000-0000-000000000009',     N'30000000-0000-0000-0000-000000000003', N'Quận / Huyện',      N'/huyen',        N'HUYEN',     2),
        (N'40000000-0000-0000-0000-00000000000a',  N'30000000-0000-0000-0000-000000000004',    N'Thông báo',         N'/notification', N'THONGBAO',  1),
        (N'40000000-0000-0000-0000-00000000000b',   N'30000000-0000-0000-0000-000000000004',    N'Tài khoản',         N'/profile',      N'PROFILE',   2)
    ) AS v(id, mid, name, url, code, ord)
    WHERE NOT EXISTS (SELECT 1 FROM [Operation] o WHERE o.[Id] = CAST(v.id AS uniqueidentifier));

    -- ============ ROLEOPERATION: Admin được phép tất cả ============
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM [Operation] op
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleOperation] ro
        WHERE ro.[RoleId] = CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
    );

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519041320_SeedAdminMenu')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519041320_SeedAdminMenu', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050000_SeedSystemRoles')
BEGIN

    INSERT INTO [Role] ([Id], [Name], [Code], [Type], [IsActive], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), v.name, v.code, v.type, 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'22222222-0000-0000-0000-000000000001',     N'Doanh nghiệp',     N'DoanhNghiep',     N'DoanhNghiep'),
        (N'22222222-0000-0000-0000-000000000002',   N'Chuyên viên sở',   N'ChuyenVienSo',    N'CanBo'),
        (N'22222222-0000-0000-0000-000000000003',  N'Trưởng phòng sở',  N'TruongPhongSo',   N'CanBo'),
        (N'22222222-0000-0000-0000-000000000004',      N'Lãnh đạo sở',      N'LanhDaoSo',       N'CanBo'),
        (N'22222222-0000-0000-0000-000000000005',  N'Chuyên viên cục',  N'ChuyenVienCuc',   N'CanBo'),
        (N'22222222-0000-0000-0000-000000000006', N'Trưởng phòng cục', N'TruongPhongCuc',  N'CanBo'),
        (N'22222222-0000-0000-0000-000000000007',     N'Lãnh đạo cục',     N'LanhDaoCuc',      N'CanBo')
    ) AS v(id, name, code, type)
    WHERE NOT EXISTS (SELECT 1 FROM [Role] r WHERE r.[Code] = v.code AND r.[IsDeleted] = 0);

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050000_SeedSystemRoles')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519050000_SeedSystemRoles', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050001_SeedDepartments')
BEGIN

    INSERT INTO [Department] ([Id], [Name], [ShortName], [Code], [Level], [Loai], [IsActive], [Priority], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), v.name, v.shortname, v.code, 1, N'KhoiCoQuanChiNhanh', 1, 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        -- ===== Cấp Trung ương =====
        (N'33000000-0000-0000-0000-000000000000', N'Cục Thương mại điện tử và Kinh tế số',                N'Cục TMĐT & KTS',     N'CUC_TMDT_KTS'),

        -- ===== 6 thành phố trực thuộc Trung ương =====
        (N'33000000-0000-0000-0000-000000000001', N'Sở Công Thương thành phố Hà Nội',                     N'SCT Hà Nội',         N'SCT_HN'),
        (N'33000000-0000-0000-0000-000000000002', N'Sở Công Thương Thành phố Hồ Chí Minh',                N'SCT TP.HCM',         N'SCT_HCM'),
        (N'33000000-0000-0000-0000-000000000003', N'Sở Công Thương thành phố Hải Phòng',                  N'SCT Hải Phòng',      N'SCT_HP'),
        (N'33000000-0000-0000-0000-000000000004', N'Sở Công Thương thành phố Đà Nẵng',                    N'SCT Đà Nẵng',        N'SCT_DN'),
        (N'33000000-0000-0000-0000-000000000005', N'Sở Công Thương thành phố Cần Thơ',                    N'SCT Cần Thơ',        N'SCT_CT'),
        (N'33000000-0000-0000-0000-000000000006', N'Sở Công Thương thành phố Huế',                        N'SCT Huế',            N'SCT_HUE'),

        -- ===== 9 tỉnh giữ nguyên (không sáp nhập) =====
        (N'33000000-0000-0000-0000-000000000007', N'Sở Công Thương tỉnh Cao Bằng',                        N'SCT Cao Bằng',       N'SCT_CB'),
        (N'33000000-0000-0000-0000-000000000008', N'Sở Công Thương tỉnh Lạng Sơn',                        N'SCT Lạng Sơn',       N'SCT_LS'),
        (N'33000000-0000-0000-0000-000000000009', N'Sở Công Thương tỉnh Lai Châu',                        N'SCT Lai Châu',       N'SCT_LCH'),
        (N'33000000-0000-0000-0000-00000000000a', N'Sở Công Thương tỉnh Điện Biên',                       N'SCT Điện Biên',      N'SCT_DB'),
        (N'33000000-0000-0000-0000-00000000000b', N'Sở Công Thương tỉnh Sơn La',                          N'SCT Sơn La',         N'SCT_SL'),
        (N'33000000-0000-0000-0000-00000000000c', N'Sở Công Thương tỉnh Quảng Ninh',                      N'SCT Quảng Ninh',     N'SCT_QN'),
        (N'33000000-0000-0000-0000-00000000000d', N'Sở Công Thương tỉnh Thanh Hóa',                       N'SCT Thanh Hóa',      N'SCT_TH'),
        (N'33000000-0000-0000-0000-00000000000e', N'Sở Công Thương tỉnh Nghệ An',                         N'SCT Nghệ An',        N'SCT_NA'),
        (N'33000000-0000-0000-0000-00000000000f', N'Sở Công Thương tỉnh Hà Tĩnh',                         N'SCT Hà Tĩnh',        N'SCT_HT'),

        -- ===== 19 tỉnh hình thành sau sáp nhập =====
        (N'33000000-0000-0000-0000-000000000010', N'Sở Công Thương tỉnh Tuyên Quang',                     N'SCT Tuyên Quang',    N'SCT_TQ'),
        (N'33000000-0000-0000-0000-000000000011', N'Sở Công Thương tỉnh Lào Cai',                         N'SCT Lào Cai',        N'SCT_LC'),
        (N'33000000-0000-0000-0000-000000000012', N'Sở Công Thương tỉnh Thái Nguyên',                     N'SCT Thái Nguyên',    N'SCT_TN'),
        (N'33000000-0000-0000-0000-000000000013', N'Sở Công Thương tỉnh Phú Thọ',                         N'SCT Phú Thọ',        N'SCT_PT'),
        (N'33000000-0000-0000-0000-000000000014', N'Sở Công Thương tỉnh Bắc Ninh',                        N'SCT Bắc Ninh',       N'SCT_BN'),
        (N'33000000-0000-0000-0000-000000000015', N'Sở Công Thương tỉnh Hưng Yên',                        N'SCT Hưng Yên',       N'SCT_HY'),
        (N'33000000-0000-0000-0000-000000000016', N'Sở Công Thương tỉnh Ninh Bình',                       N'SCT Ninh Bình',      N'SCT_NB'),
        (N'33000000-0000-0000-0000-000000000017', N'Sở Công Thương tỉnh Quảng Trị',                       N'SCT Quảng Trị',      N'SCT_QT'),
        (N'33000000-0000-0000-0000-000000000018', N'Sở Công Thương tỉnh Quảng Ngãi',                      N'SCT Quảng Ngãi',     N'SCT_QNG'),
        (N'33000000-0000-0000-0000-000000000019', N'Sở Công Thương tỉnh Gia Lai',                         N'SCT Gia Lai',        N'SCT_GL'),
        (N'33000000-0000-0000-0000-00000000001a', N'Sở Công Thương tỉnh Khánh Hòa',                       N'SCT Khánh Hòa',      N'SCT_KH'),
        (N'33000000-0000-0000-0000-00000000001b', N'Sở Công Thương tỉnh Lâm Đồng',                        N'SCT Lâm Đồng',       N'SCT_LD'),
        (N'33000000-0000-0000-0000-00000000001c', N'Sở Công Thương tỉnh Đắk Lắk',                         N'SCT Đắk Lắk',        N'SCT_DLK'),
        (N'33000000-0000-0000-0000-00000000001d', N'Sở Công Thương tỉnh Đồng Nai',                        N'SCT Đồng Nai',       N'SCT_DNI'),
        (N'33000000-0000-0000-0000-00000000001e', N'Sở Công Thương tỉnh Tây Ninh',                        N'SCT Tây Ninh',       N'SCT_TNI'),
        (N'33000000-0000-0000-0000-00000000001f', N'Sở Công Thương tỉnh Vĩnh Long',                       N'SCT Vĩnh Long',      N'SCT_VL'),
        (N'33000000-0000-0000-0000-000000000020', N'Sở Công Thương tỉnh Đồng Tháp',                       N'SCT Đồng Tháp',      N'SCT_DT'),
        (N'33000000-0000-0000-0000-000000000021', N'Sở Công Thương tỉnh Cà Mau',                          N'SCT Cà Mau',         N'SCT_CM'),
        (N'33000000-0000-0000-0000-000000000022', N'Sở Công Thương tỉnh An Giang',                        N'SCT An Giang',       N'SCT_AG')
    ) AS v(id, name, shortname, code)
    WHERE NOT EXISTS (SELECT 1 FROM [Department] d WHERE d.[Code] = v.code AND d.[IsDeleted] = 0);

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050001_SeedDepartments')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519050001_SeedDepartments', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050002_SeedPlatformContractMenu')
BEGIN

    -- ============ MODULES ============
    INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), v.code, v.name, v.ord, 1, v.icon, v.icon, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'30000000-0000-0000-0000-000000000010', N'NENTANG', N'Quản lý nền tảng TMĐT',          10, N'<AppstoreOutlined />'),
        (N'30000000-0000-0000-0000-000000000011', N'HOPDONG', N'Chứng thực hợp đồng điện tử',    11, N'<FileProtectOutlined />')
    ) AS v(id, code, name, ord, icon)
    WHERE NOT EXISTS (SELECT 1 FROM [Module] m WHERE m.[Id] = CAST(v.id AS uniqueidentifier));

    -- ============ OPERATIONS ============
    INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(v.id AS uniqueidentifier), CAST(v.mid AS uniqueidentifier), v.name, v.url, v.code, v.ord, 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        -- Nền tảng
        (N'40000000-0000-0000-0000-000000000100',   N'30000000-0000-0000-0000-000000000010', N'Đăng ký nền tảng',         N'/nen-tang/dang-ky',  N'NT_DANGKY',   1),
        (N'40000000-0000-0000-0000-000000000101', N'30000000-0000-0000-0000-000000000010', N'Danh sách nền tảng',       N'/nen-tang',          N'NT_DANHSACH', 2),
        (N'40000000-0000-0000-0000-000000000102',     N'30000000-0000-0000-0000-000000000010', N'Xử lý hồ sơ nền tảng',     N'/nen-tang/xu-ly',    N'NT_XULY',     3),
        -- Hợp đồng điện tử
        (N'40000000-0000-0000-0000-000000000200',   N'30000000-0000-0000-0000-000000000011', N'Đăng ký chứng thực HĐĐT',  N'/hop-dong/dang-ky',  N'HD_DANGKY',   1),
        (N'40000000-0000-0000-0000-000000000201', N'30000000-0000-0000-0000-000000000011', N'Danh sách hợp đồng',       N'/hop-dong',          N'HD_DANHSACH', 2),
        (N'40000000-0000-0000-0000-000000000202',     N'30000000-0000-0000-0000-000000000011', N'Xử lý hồ sơ hợp đồng',     N'/hop-dong/xu-ly',    N'HD_XULY',     3)
    ) AS v(id, mid, name, url, code, ord)
    WHERE NOT EXISTS (SELECT 1 FROM [Operation] o WHERE o.[Id] = CAST(v.id AS uniqueidentifier));

    -- ============ ROLEOPERATION ============
    -- Admin: full quyền 6 operation
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM [Operation] op
    WHERE op.[Id] IN (
        CAST(N'40000000-0000-0000-0000-000000000100' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000101' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000102' AS uniqueidentifier),
        CAST(N'40000000-0000-0000-0000-000000000200' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000201' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000202' AS uniqueidentifier)
    )
    AND NOT EXISTS (
        SELECT 1 FROM [RoleOperation] ro
        WHERE ro.[RoleId] = CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
    );

    -- Doanh nghiệp: chỉ thấy form đăng ký và danh sách của mình
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM [Operation] op
    WHERE op.[Id] IN (
        CAST(N'40000000-0000-0000-0000-000000000100' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000101' AS uniqueidentifier),
        CAST(N'40000000-0000-0000-0000-000000000200' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000201' AS uniqueidentifier)
    )
    AND NOT EXISTS (
        SELECT 1 FROM [RoleOperation] ro
        WHERE ro.[RoleId] = CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
    );

    -- Các role cán bộ (Sở + Cục): thấy danh sách + xử lý (cả nền tảng + hợp đồng)
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(r.role_id AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM [Operation] op
    CROSS JOIN (VALUES
        (N'22222222-0000-0000-0000-000000000002'),
        (N'22222222-0000-0000-0000-000000000003'),
        (N'22222222-0000-0000-0000-000000000004'),
        (N'22222222-0000-0000-0000-000000000005'),
        (N'22222222-0000-0000-0000-000000000006'),
        (N'22222222-0000-0000-0000-000000000007')
    ) AS r(role_id)
    WHERE op.[Id] IN (
        CAST(N'40000000-0000-0000-0000-000000000101' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000102' AS uniqueidentifier),
        CAST(N'40000000-0000-0000-0000-000000000201' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000202' AS uniqueidentifier)
    )
    AND NOT EXISTS (
        SELECT 1 FROM [RoleOperation] ro
        WHERE ro.[RoleId] = CAST(r.role_id AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
    );

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050002_SeedPlatformContractMenu')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519050002_SeedPlatformContractMenu', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050003_SeedTestUsers')
BEGIN

    -- ============ ASPNETUSERS ============
    INSERT INTO [AspNetUsers] (
        [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
        [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
        [Name], [Gender], [Type], [DonViId],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(v.id AS uniqueidentifier), v.username, UPPER(v.username), v.email, UPPER(v.email),
        1, N'AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==', CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
        0, 0, 1, 0,
        v.fullname, 0, v.type, CAST(NULLIF(NULLIF(v.donvi, N''), N'00000000-0000-0000-0000-000000000000') AS uniqueidentifier),
        SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'44444444-0000-0000-0000-000000000001',     N'doanhnghiep',    N'doanhnghiep@test.local',    N'Doanh nghiệp Test',          N'DoanhNghiep', N'00000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000002',   N'chuyenvienso',   N'chuyenvienso@test.local',   N'Chuyên viên Sở Hà Nội',     N'CanBo',       N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000003',  N'truongphongso',  N'truongphongso@test.local',  N'Trưởng phòng Sở Hà Nội',    N'CanBo',       N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000004',      N'lanhdaoso',      N'lanhdaoso@test.local',      N'Lãnh đạo Sở Hà Nội',        N'CanBo',       N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000005',  N'chuyenviencuc',  N'chuyenviencuc@test.local',  N'Chuyên viên Cục TMĐT',      N'CanBo',       N'33000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000006', N'truongphongcuc', N'truongphongcuc@test.local', N'Trưởng phòng Cục TMĐT',     N'CanBo',       N'33000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000007',     N'lanhdaocuc',     N'lanhdaocuc@test.local',     N'Lãnh đạo Cục TMĐT',         N'CanBo',       N'33000000-0000-0000-0000-000000000000')
    ) AS v(id, username, email, fullname, type, donvi)
    WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[NormalizedUserName] = UPPER(v.username));

    -- ============ USERROLE ============
    INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(v.uid AS uniqueidentifier), CAST(v.rid AS uniqueidentifier), CAST(v.did AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'44444444-0000-0000-0000-000000000001',     N'22222222-0000-0000-0000-000000000001',     N'00000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000002',   N'22222222-0000-0000-0000-000000000002',   N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000003',  N'22222222-0000-0000-0000-000000000003',  N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000004',      N'22222222-0000-0000-0000-000000000004',      N'33000000-0000-0000-0000-000000000001'),
        (N'44444444-0000-0000-0000-000000000005',  N'22222222-0000-0000-0000-000000000005',  N'33000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000006', N'22222222-0000-0000-0000-000000000006', N'33000000-0000-0000-0000-000000000000'),
        (N'44444444-0000-0000-0000-000000000007',     N'22222222-0000-0000-0000-000000000007',     N'33000000-0000-0000-0000-000000000000')
    ) AS v(uid, rid, did)
    WHERE NOT EXISTS (
        SELECT 1 FROM [UserRole] ur
        WHERE ur.[UserId] = CAST(v.uid AS uniqueidentifier) AND ur.[RoleId] = CAST(v.rid AS uniqueidentifier)
    );

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519050003_SeedTestUsers')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519050003_SeedTestUsers', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519072359_AddCompanyInfo')
BEGIN
    CREATE TABLE [CompanyInfo] (
        [Id] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Name] nvarchar(max) NULL,
        [EnglishName] nvarchar(max) NULL,
        [ShortName] nvarchar(max) NULL,
        [TaxCode] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [CityId] nvarchar(max) NULL,
        [CityName] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Fax] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [RepresenterName] nvarchar(max) NULL,
        [RepresenterMobile] nvarchar(max) NULL,
        [RepresenterPhone] nvarchar(max) NULL,
        [RepresenterEmail] nvarchar(max) NULL,
        [RepresenterCCCD] nvarchar(max) NULL,
        [Detail] nvarchar(max) NULL,
        [ApproveDateOnline] datetime2 NULL,
        [TinhId] uniqueidentifier NULL,
        [TinhIdNew] uniqueidentifier NULL,
        [XaId] uniqueidentifier NULL,
        [QuocGiaId] nvarchar(max) NULL,
        [TypeOrganization] nvarchar(max) NULL,
        [IsNuocNgoai] bit NOT NULL,
        [IsVonDauTuNuocNgoai] bit NOT NULL,
        [DKKD] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_CompanyInfo] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519072359_AddCompanyInfo')
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''30cd8500-dd6b-42a1-ae24-4d77a4d50451'', [PasswordHash] = N''AQAAAAIAAYagAAAAEOZrlXxI1ifte0PI9QHp8lCqMWvwr5nEBGSRE4aFzmQqRvMipBvA52iIlPIbJpj/3Q=='', [SecurityStamp] = N''b086b40f-671f-4a14-86a5-d7cefe27df44''
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519072359_AddCompanyInfo')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519072359_AddCompanyInfo', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519080000_SeedCompanyInfoMenu')
BEGIN

    -- ============ MODULE ============
    INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(N'30000000-0000-0000-0000-000000000020' AS uniqueidentifier), N'QLDN', N'Quản lý doanh nghiệp', 5, 1, N'<BankOutlined />', N'<BankOutlined />', SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [Module] WHERE [Id] = CAST(N'30000000-0000-0000-0000-000000000020' AS uniqueidentifier));

    -- ============ OPERATION ============
    INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT CAST(N'40000000-0000-0000-0000-000000000300' AS uniqueidentifier), CAST(N'30000000-0000-0000-0000-000000000020' AS uniqueidentifier), N'Doanh nghiệp', N'/QLDoanhNghiep', N'QLDOANHNGHIEP', 1, 1, SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [Operation] WHERE [Id] = CAST(N'40000000-0000-0000-0000-000000000300' AS uniqueidentifier));

    -- ============ ROLEOPERATION: Admin được quyền truy cập ============
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier), CAST(N'40000000-0000-0000-0000-000000000300' AS uniqueidentifier), 1, SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleOperation]
        WHERE [RoleId] = CAST(N'22222222-2222-2222-2222-222222222222' AS uniqueidentifier) AND [OperationId] = CAST(N'40000000-0000-0000-0000-000000000300' AS uniqueidentifier)
    );

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519080000_SeedCompanyInfoMenu')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519080000_SeedCompanyInfoMenu', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519083023_add_class_PlatformManage')
BEGIN
    CREATE TABLE [PlatformManage] (
        [Id] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [AppManageTypeId] int NULL,
        [OrganizationId] bigint NULL,
        [TypeOrganization] nvarchar(max) NULL,
        [CompanyName] nvarchar(max) NULL,
        [CompanyTaxCode] nvarchar(max) NULL,
        [CompanyAddress] nvarchar(max) NULL,
        [CompanyPhone] nvarchar(max) NULL,
        [CompanyFax] nvarchar(max) NULL,
        [Name] nvarchar(max) NULL,
        [Domain] nvarchar(max) NULL,
        [ISPId] nvarchar(max) NULL,
        [StaffNumber] int NULL,
        [RepresenterName] nvarchar(max) NULL,
        [RepresenterMobile] nvarchar(max) NULL,
        [RepresenterJob] nvarchar(max) NULL,
        [RepresenterEmail] nvarchar(max) NULL,
        [Detail] nvarchar(max) NULL,
        [SubmitDate] datetime2 NULL,
        [SubmitName] nvarchar(max) NULL,
        [ReviewDate] datetime2 NULL,
        [ReviewName] nvarchar(max) NULL,
        [RequestChangeDate] datetime2 NULL,
        [RequestChangeName] nvarchar(max) NULL,
        [WebsiteNumber] nvarchar(max) NULL,
        [Seal] nvarchar(max) NULL,
        [DomainAdd] nvarchar(max) NULL,
        [ImagePath] nvarchar(max) NULL,
        [TinhId] nvarchar(max) NULL,
        [XaId] nvarchar(max) NULL,
        [QuocGiaId] nvarchar(max) NULL,
        [TienIchKhac] nvarchar(max) NULL,
        [LoaiHangHoaKhac] nvarchar(max) NULL,
        [OldSysId] bigint NULL,
        [DateLine] datetime2 NULL,
        [Address] nvarchar(max) NULL,
        [PhuongThucLienHe] nvarchar(max) NULL,
        [RepresenterNameOnline] nvarchar(max) NULL,
        [RepresenterJobOnline] nvarchar(max) NULL,
        [RepresenterMobileOnline] nvarchar(max) NULL,
        [RepresenterEmailOnline] nvarchar(max) NULL,
        [AddressOnline] nvarchar(max) NULL,
        [PhuongThucLienHeOnline] nvarchar(max) NULL,
        [RepresenterNameDaiDien] nvarchar(max) NULL,
        [RepresenterJobDaiDien] nvarchar(max) NULL,
        [RepresenterMobileDaiDien] nvarchar(max) NULL,
        [RepresenterEmailDaiDien] nvarchar(max) NULL,
        [AddressDaiDien] nvarchar(max) NULL,
        [PhuongThucLienHeDaiDien] nvarchar(max) NULL,
        [API] nvarchar(max) NULL,
        [URLApp] nvarchar(max) NULL,
        [UserApp] nvarchar(max) NULL,
        [PassApp] nvarchar(max) NULL,
        [IsNuocNgoai] bit NULL,
        [SoCongVan] nvarchar(max) NULL,
        [CCCD] nvarchar(max) NULL,
        [RepresenterCCCD] nvarchar(max) NULL,
        [DataSignedUser] nvarchar(max) NULL,
        [SignDateUser] datetime2 NULL,
        [ISPIdKhac] nvarchar(max) NULL,
        [TinhIdNew] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedId] uniqueidentifier NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdatedId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedDate] datetime2 NULL,
        [DeletedId] uniqueidentifier NULL,
        CONSTRAINT [PK_PlatformManage] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519083023_add_class_PlatformManage')
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''ed988174-d99c-41f1-a844-8a0c7931dcd8'', [PasswordHash] = N''AQAAAAIAAYagAAAAEDk4MbtTJQ+dtf67aGmm3DWXbf+N33xxPjn3NhZR4dn/UpWJpTgjTwXpOIXfRx+XSw=='', [SecurityStamp] = N''1ef5a021-6bd9-4bf6-81b7-8d1176355b80''
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519083023_add_class_PlatformManage')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519083023_add_class_PlatformManage', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519090000_SeedDoanhNghiepTestAccounts')
BEGIN

    -- ============ ASPNETUSERS ============
    INSERT INTO [AspNetUsers] (
        [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
        [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
        [Name], [Gender], [Type], [IsSSO],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(v.id AS uniqueidentifier), v.username, UPPER(v.username), v.email, UPPER(v.email),
        1, N'AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==', CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
        0, 0, 1, 0,
        v.fullname, 0, N'DoanhNghiep', 0,
        SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'55555555-0000-0000-0000-000000000001',       N'dn_abc',       N'lienhe@congtyabc.vn',    N'Công ty TNHH ABC'),
        (N'55555555-0000-0000-0000-000000000002',   N'dn_xyz',       N'admin@xyzgroup.vn',      N'Công ty CP XYZ (FDI)'),
        (N'55555555-0000-0000-0000-000000000003',    N'dn_canhan',    N'nguyenvana@gmail.com',   N'Nguyễn Văn A (Cá nhân)'),
        (N'55555555-0000-0000-0000-000000000004', N'dn_global',    N'sea@globaltradeasia.com',N'Global Trade Asia Pte Ltd')
    ) AS v(id, username, email, fullname)
    WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[NormalizedUserName] = UPPER(v.username));

    -- ============ USERROLE: gán role DoanhNghiep ============
    INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(v.uid AS uniqueidentifier), CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier), CAST(N'00000000-0000-0000-0000-000000000000' AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'55555555-0000-0000-0000-000000000001'),
        (N'55555555-0000-0000-0000-000000000002'),
        (N'55555555-0000-0000-0000-000000000003'),
        (N'55555555-0000-0000-0000-000000000004')
    ) AS v(uid)
    WHERE NOT EXISTS (
        SELECT 1 FROM [UserRole] ur
        WHERE ur.[UserId] = CAST(v.uid AS uniqueidentifier) AND ur.[RoleId] = CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier)
    );

    -- ============ COMPANYINFO ============
    -- 1. Công ty TNHH ABC (Company, trong nước, Hà Nội)
    INSERT INTO [CompanyInfo] (
        [Id], [Status], [Name], [EnglishName], [ShortName], [TaxCode],
        [Address], [CityId], [CityName], [TinhIdNew],
        [Phone], [Fax], [Email],
        [RepresenterName], [RepresenterMobile], [RepresenterPhone], [RepresenterEmail], [RepresenterCCCD],
        [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
        [QuocGiaId], [Detail], [DKKD],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(N'66666666-0000-0000-0000-000000000001' AS uniqueidentifier), 1, N'Công ty TNHH ABC', N'ABC Co., Ltd', N'ABC', N'0100123456',
        N'Số 1 Trần Hưng Đạo, Hoàn Kiếm', N'1', N'Hà Nội', CAST(N'33000000-0000-0000-0000-000000000001' AS uniqueidentifier),
        N'024.39001234', N'024.39001235', N'lienhe@congtyabc.vn',
        N'Nguyễn Văn An', N'0901111111', N'024.39001234', N'an.nguyen@congtyabc.vn', N'001190001234',
        N'Company', 0, 0,
        N'VN', N'Doanh nghiệp trong nước hoạt động TMĐT', N'/uploads/dkkd/abc.pdf',
        SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'66666666-0000-0000-0000-000000000001' AS uniqueidentifier));

    -- 2. Công ty CP XYZ (Company, có vốn FDI)
    INSERT INTO [CompanyInfo] (
        [Id], [Status], [Name], [EnglishName], [ShortName], [TaxCode],
        [Address], [CityId], [CityName], [TinhIdNew],
        [Phone], [Email],
        [RepresenterName], [RepresenterMobile], [RepresenterEmail], [RepresenterCCCD],
        [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
        [QuocGiaId], [DKKD],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(N'66666666-0000-0000-0000-000000000002' AS uniqueidentifier), 1, N'Công ty Cổ phần XYZ', N'XYZ Joint Stock Company', N'XYZ', N'0300999888',
        N'Lô A2 KCN Tân Bình, Phường 15', N'50', N'TP.HCM', CAST(N'33000000-0000-0000-0000-000000000001' AS uniqueidentifier),
        N'028.38003344', N'admin@xyzgroup.vn',
        N'Trần Thị Bình', N'0902222222', N'binh.tran@xyzgroup.vn', N'079190002222',
        N'Company', 0, 1,
        N'VN', N'/uploads/dkkd/xyz.pdf',
        SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'66666666-0000-0000-0000-000000000002' AS uniqueidentifier));

    -- 3. Cá nhân kinh doanh (Personal)
    INSERT INTO [CompanyInfo] (
        [Id], [Status], [Name], [TaxCode],
        [Address], [CityId], [CityName], [TinhIdNew],
        [Phone], [Email],
        [RepresenterName], [RepresenterMobile], [RepresenterEmail], [RepresenterCCCD],
        [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
        [QuocGiaId],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(N'66666666-0000-0000-0000-000000000003' AS uniqueidentifier), 1, N'Nguyễn Văn A', N'8123456789',
        N'15 Phố Huế, Hai Bà Trưng', N'1', N'Hà Nội', CAST(N'33000000-0000-0000-0000-000000000001' AS uniqueidentifier),
        NULL, N'nguyenvana@gmail.com',
        N'Nguyễn Văn A', N'0903333333', N'nguyenvana@gmail.com', N'001190003333',
        N'Personal', 0, 0,
        N'VN',
        SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'66666666-0000-0000-0000-000000000003' AS uniqueidentifier));

    -- 4. Tổ chức nước ngoài (Organization, IsNuocNgoai)
    INSERT INTO [CompanyInfo] (
        [Id], [Status], [Name], [EnglishName], [TaxCode],
        [Address], [CityName],
        [Phone], [Email],
        [RepresenterName], [RepresenterMobile], [RepresenterEmail],
        [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
        [QuocGiaId],
        [CreatedDate], [UpdatedDate], [IsDeleted]
    )
    SELECT
        CAST(N'66666666-0000-0000-0000-000000000004' AS uniqueidentifier), 1, N'Global Trade Asia Pte Ltd', N'Global Trade Asia Pte Ltd', N'SG-201912345R',
        N'8 Marina Boulevard, Marina Bay Financial Centre', N'Singapore',
        N'+65 6123 4567', N'sea@globaltradeasia.com',
        N'John Smith', N'+6591234567', N'john.smith@globaltradeasia.com',
        N'Organization', 1, 1,
        N'SG',
        SYSDATETIME(), SYSDATETIME(), 0
    WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'66666666-0000-0000-0000-000000000004' AS uniqueidentifier));

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519090000_SeedDoanhNghiepTestAccounts')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519090000_SeedDoanhNghiepTestAccounts', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519100000_GrantDoanhNghiepBasicMenu')
BEGIN

    -- Cấp quyền cho role DoanhNghiep truy cập Thông báo + Tài khoản cá nhân
    INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
    SELECT NEWID(), CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier), CAST(v.op AS uniqueidentifier), 1, SYSDATETIME(), SYSDATETIME(), 0
    FROM (VALUES
        (N'40000000-0000-0000-0000-00000000000a'),
        (N'40000000-0000-0000-0000-00000000000b')
    ) AS v(op)
    WHERE NOT EXISTS (
        SELECT 1 FROM [RoleOperation] ro
        WHERE ro.[RoleId] = CAST(N'22222222-0000-0000-0000-000000000001' AS uniqueidentifier) AND ro.[OperationId] = CAST(v.op AS uniqueidentifier)
    );

END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260519100000_GrantDoanhNghiepBasicMenu')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260519100000_GrantDoanhNghiepBasicMenu', N'7.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    ALTER TABLE [TaiLieuDinhKem] ADD [CoChuKySo] bit NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    ALTER TABLE [TaiLieuDinhKem] ADD [DonViPhatHanh] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    ALTER TABLE [TaiLieuDinhKem] ADD [IsKySo] bit NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    ALTER TABLE [TaiLieuDinhKem] ADD [NgayKy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    ALTER TABLE [TaiLieuDinhKem] ADD [NguoiKy] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''4797334c-789a-44ee-9aab-26bb4787babe'', [PasswordHash] = N''AQAAAAIAAYagAAAAEIGQyxefovd0J7Droo/w3ERUiuF1dJsODyfs7a7HwS7soszbaT5AMty5fS0Ifpb83w=='', [SecurityStamp] = N''647c23b9-85c8-4092-8aa0-c22192791edb''
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260520025738_AddFileServerColumnsToTaiLieuDinhKem', N'7.0.18');
END;
GO

COMMIT;
GO


GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'rental.Rooms', N'U') IS NULL
BEGIN
IF SCHEMA_ID(N'rental') IS NULL EXEC(N'CREATE SCHEMA [rental];');

CREATE TABLE [rental].[Appointments] (
    [Id] uniqueidentifier NOT NULL,
    [ThreadId] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [VisitAt] datetimeoffset NOT NULL,
    [SourceMessageId] nvarchar(250) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[Deliveries] (
    [Id] uniqueidentifier NOT NULL,
    [PostId] uniqueidentifier NOT NULL,
    [GroupId] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Detail] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Deliveries] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[Groups] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Url] nvarchar(400) NOT NULL,
    [Enabled] bit NOT NULL,
    CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[Posts] (
    [Id] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [TelegramUpdateId] bigint NULL,
    [Content] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Posts] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[Rooms] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Address] nvarchar(400) NOT NULL,
    [MonthlyRent] decimal(18,0) NOT NULL,
    [Details] nvarchar(max) NOT NULL,
    [Available] bit NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[State] (
    [Id] int NOT NULL IDENTITY,
    [TelegramOffset] bigint NOT NULL,
    [Paused] bit NOT NULL,
    [LastPostAt] datetimeoffset NULL,
    CONSTRAINT [PK_State] PRIMARY KEY ([Id])
);

CREATE TABLE [rental].[Threads] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerName] nvarchar(150) NOT NULL,
    [Url] nvarchar(400) NOT NULL,
    [AutoReply] bit NOT NULL,
    [LastMessageId] nvarchar(max) NOT NULL,
    [LastReply] nvarchar(max) NOT NULL,
    [LastError] nvarchar(max) NOT NULL,
    [PendingAppointment] nvarchar(max) NOT NULL,
    [LastCheckedAt] datetimeoffset NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Threads] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Appointments_ThreadId_SourceMessageId] ON [rental].[Appointments] ([ThreadId], [SourceMessageId]);

CREATE UNIQUE INDEX [IX_Deliveries_PostId_GroupId] ON [rental].[Deliveries] ([PostId], [GroupId]);

CREATE UNIQUE INDEX [IX_Groups_Url] ON [rental].[Groups] ([Url]);

CREATE UNIQUE INDEX [IX_Posts_TelegramUpdateId] ON [rental].[Posts] ([TelegramUpdateId]) WHERE [TelegramUpdateId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Threads_Url] ON [rental].[Threads] ([Url]);

END;
IF NOT EXISTS (SELECT 1 FROM [rental].[State]) BEGIN SET IDENTITY_INSERT [rental].[State] ON; INSERT INTO [rental].[State] ([Id], [TelegramOffset], [Paused]) VALUES (1, 0, 1); SET IDENTITY_INSERT [rental].[State] OFF; END;
COMMIT;
GO
SELECT DB_NAME() AS DatabaseName, UserName FROM dbo.AspNetUsers WHERE NormalizedUserName = N'ADMIN';
