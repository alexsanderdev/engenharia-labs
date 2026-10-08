-- =============================================================================
-- Parte A — DDL do OrderFlow (SOLUÇÃO)
-- Executado UMA vez num banco vazio (F3M01Modelagem) pela fixture dos testes.
-- Pode usar GO para separar lotes.
-- =============================================================================

-- Clientes: chave surrogate (IDENTITY) + chave natural protegida por UNIQUE (Email).
-- IndicadoPorId é um auto-relacionamento opcional (NULL = ninguém indicou).
CREATE TABLE dbo.Clientes
(
    Id            int            IDENTITY(1,1) NOT NULL,
    Nome          nvarchar(150)  NOT NULL,
    Email         nvarchar(254)  NOT NULL,
    IndicadoPorId int            NULL,
    CriadoEm      datetime2(3)   NOT NULL CONSTRAINT DF_Clientes_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Clientes_Email UNIQUE (Email),
    CONSTRAINT FK_Clientes_IndicadoPor FOREIGN KEY (IndicadoPorId) REFERENCES dbo.Clientes (Id)
);

-- Produtos: SKU é código ASCII (varchar); nome tem acentos (nvarchar).
CREATE TABLE dbo.Produtos
(
    Id    int            IDENTITY(1,1) NOT NULL,
    Sku   varchar(20)    NOT NULL,
    Nome  nvarchar(200)  NOT NULL,
    Preco decimal(18,2)  NOT NULL,
    Ativo bit            NOT NULL CONSTRAINT DF_Produtos_Ativo DEFAULT (1),

    CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Produtos_Sku UNIQUE (Sku),
    CONSTRAINT CK_Produtos_Preco CHECK (Preco >= 0)
);

-- Pedidos: Total é desnormalização consciente (soma dos itens gravada no fechamento).
CREATE TABLE dbo.Pedidos
(
    Id        int            IDENTITY(1,1) NOT NULL,
    ClienteId int            NOT NULL,
    CriadoEm  datetime2(3)   NOT NULL CONSTRAINT DF_Pedidos_CriadoEm DEFAULT (SYSUTCDATETIME()),
    Status    varchar(20)    NOT NULL CONSTRAINT DF_Pedidos_Status DEFAULT ('Created'),
    Total     decimal(18,2)  NOT NULL CONSTRAINT DF_Pedidos_Total DEFAULT (0),

    CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
    CONSTRAINT CK_Pedidos_Status CHECK (Status IN ('Created', 'Confirmed', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Pedidos_Total CHECK (Total >= 0)
);

-- ItensPedido: chave composta (um produto aparece uma vez por pedido).
-- PrecoUnitario é o preço NO MOMENTO da compra (fato histórico, não cópia redundante).
CREATE TABLE dbo.ItensPedido
(
    PedidoId      int            NOT NULL,
    ProdutoId     int            NOT NULL,
    Quantidade    int            NOT NULL,
    PrecoUnitario decimal(18,2)  NOT NULL,

    CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (PedidoId, ProdutoId),
    CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
    CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
    CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
);
GO

-- Índice na FK de ItensPedido.ProdutoId (a PK já cobre PedidoId). Detalhes no módulo 3.02.
CREATE INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);
CREATE INDEX IX_Pedidos_ClienteId ON dbo.Pedidos (ClienteId);
GO
