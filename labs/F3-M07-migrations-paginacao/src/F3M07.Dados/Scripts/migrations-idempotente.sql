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
    WHERE [MigrationId] = N'20261008194028_Inicial'
)
BEGIN
    CREATE TABLE [Clientes] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194028_Inicial'
)
BEGIN
    CREATE TABLE [Pedidos] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [Versao] rowversion NOT NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pedidos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194028_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194028_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_CriadoEm_Id] ON [Pedidos] ([CriadoEm] DESC, [Id] DESC) INCLUDE ([ClienteId], [Status], [Total]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194028_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008194028_Inicial', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194036_AdicionaCanalAoPedido'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [Canal] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194036_AdicionaCanalAoPedido'
)
BEGIN
    EXEC(N'UPDATE Pedidos SET Canal = N''Legado'' WHERE Canal IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194036_AdicionaCanalAoPedido'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Pedidos]') AND [c].[name] = N'Canal');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Pedidos] DROP CONSTRAINT ' + @var + ';');
    EXEC(N'UPDATE [Pedidos] SET [Canal] = N''Web'' WHERE [Canal] IS NULL');
    ALTER TABLE [Pedidos] ALTER COLUMN [Canal] nvarchar(20) NOT NULL;
    ALTER TABLE [Pedidos] ADD DEFAULT N'Web' FOR [Canal];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008194036_AdicionaCanalAoPedido'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008194036_AdicionaCanalAoPedido', N'10.0.12');
END;

COMMIT;
GO

