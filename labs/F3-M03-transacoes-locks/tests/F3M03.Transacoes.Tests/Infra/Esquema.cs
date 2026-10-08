namespace F3M03.Transacoes.Tests.Infra;

/// <summary>Schema e dados iniciais, criados por script (iguais nos dois bancos do lab).</summary>
public static class Esquema
{
    public const string CriarTabelas =
        """
        CREATE TABLE dbo.Clientes (
            Id   int           NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
            Nome nvarchar(100) NOT NULL
        );

        CREATE TABLE dbo.Produtos (
            Id      int           NOT NULL CONSTRAINT PK_Produtos PRIMARY KEY,
            Sku     varchar(30)   NOT NULL CONSTRAINT UQ_Produtos_Sku UNIQUE,
            Nome    nvarchar(100) NOT NULL,
            Preco   decimal(18,2) NOT NULL,
            Estoque int           NOT NULL,
            Versao  rowversion    NOT NULL
        );

        CREATE TABLE dbo.Pedidos (
            Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pedidos PRIMARY KEY,
            ClienteId  int NOT NULL CONSTRAINT FK_Pedidos_Clientes REFERENCES dbo.Clientes (Id),
            ProdutoId  int NOT NULL CONSTRAINT FK_Pedidos_Produtos REFERENCES dbo.Produtos (Id),
            Quantidade int NOT NULL CONSTRAINT CK_Pedidos_Quantidade CHECK (Quantidade > 0)
        );

        CREATE INDEX IX_Pedidos_ClienteId ON dbo.Pedidos (ClienteId);
        """;

    /// <summary>
    /// Estado conhecido antes de CADA teste: 3 produtos com estoque 10 e preço fixo,
    /// 2 clientes, cliente 1 com 2 pedidos e cliente 2 com 1.
    /// </summary>
    public const string ResetarDados =
        """
        DELETE FROM dbo.Pedidos;
        DELETE FROM dbo.Produtos;
        DELETE FROM dbo.Clientes;

        INSERT dbo.Clientes (Id, Nome) VALUES (1, N'Ana'), (2, N'Bruno');

        INSERT dbo.Produtos (Id, Sku, Nome, Preco, Estoque) VALUES
            (1, 'TECLADO', N'Teclado mecânico', 250.00, 10),
            (2, 'MOUSE',   N'Mouse sem fio',    120.00, 10),
            (3, 'MONITOR', N'Monitor 27"',     1500.00, 10);

        INSERT dbo.Pedidos (ClienteId, ProdutoId, Quantidade) VALUES (1, 1, 1), (1, 2, 2), (2, 3, 1);
        """;
}
