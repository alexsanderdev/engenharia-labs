-- =============================================================================
-- Schema + massa de dados do lab de índices (PRONTO — não altere).
-- 20 mil clientes, 1 mil produtos, 200 mil pedidos, ~500 mil itens.
-- Tudo DETERMINÍSTICO (aritmética sobre uma sequência de números, nada de NEWID/RAND):
-- o mesmo dado em toda máquina = o mesmo plano em toda máquina.
--
-- Sequência de números: GENERATE_SERIES (SQL Server 2022, nível de compatibilidade 160).
-- Em versões antigas, a "tabela de números" clássica é um CROSS JOIN de dígitos:
--   WITH d(n) AS (SELECT n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) v(n))
--   SELECT a.n + 10*b.n + 100*c.n + ... FROM d a CROSS JOIN d b CROSS JOIN d c ...
-- =============================================================================
SET NOCOUNT ON;

CREATE TABLE dbo.Clientes
(
    Id        int           NOT NULL,
    Nome      nvarchar(150) NOT NULL,
    Email     nvarchar(254) NOT NULL,
    Documento varchar(11)   NOT NULL,          -- CPF só com dígitos: ASCII, por isso varchar
    CriadoEm  datetime2(3)  NOT NULL,
    CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Clientes_Email UNIQUE (Email),
    CONSTRAINT UQ_Clientes_Documento UNIQUE (Documento)
);

CREATE TABLE dbo.Produtos
(
    Id    int           NOT NULL,
    Sku   varchar(20)   NOT NULL,
    Nome  nvarchar(200) NOT NULL,
    Preco decimal(18,2) NOT NULL,
    Ativo bit           NOT NULL,
    CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Produtos_Sku UNIQUE (Sku)
);

CREATE TABLE dbo.Pedidos
(
    Id              int           NOT NULL,
    ClienteId       int           NOT NULL,
    CriadoEm        datetime2(3)  NOT NULL,
    Status          varchar(20)   NOT NULL,
    Total           decimal(18,2) NOT NULL,
    EnderecoEntrega nvarchar(300) NOT NULL,   -- coluna "larga": deixa a tabela com cara de produção
    CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
    CONSTRAINT CK_Pedidos_Status CHECK (Status IN ('Created', 'Confirmed', 'Completed', 'Cancelled'))
);

CREATE TABLE dbo.ItensPedido
(
    PedidoId      int           NOT NULL,
    ProdutoId     int           NOT NULL,
    Quantidade    int           NOT NULL,
    PrecoUnitario decimal(18,2) NOT NULL,
    CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (PedidoId, ProdutoId),
    CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id),
    CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id)
);
GO

-- 20 mil clientes. Documento = 11 dígitos únicos.
INSERT INTO dbo.Clientes WITH (TABLOCK) (Id, Nome, Email, Documento, CriadoEm)
SELECT s.value,
       CONCAT(N'Cliente ', s.value),
       CONCAT(N'cliente', s.value, N'@orderflow.dev'),
       CONVERT(varchar(11), 10000000000 + CAST(s.value AS bigint) * 104729),
       DATEADD(DAY, s.value % 700, CAST('2022-01-01' AS datetime2(3)))
FROM GENERATE_SERIES(1, 20000) AS s;

-- 1 mil produtos.
INSERT INTO dbo.Produtos WITH (TABLOCK) (Id, Sku, Nome, Preco, Ativo)
SELECT s.value,
       CONCAT('SKU-', RIGHT(CONCAT('00000', s.value), 5)),
       CONCAT(N'Produto ', s.value),
       CAST(10 + (s.value * 37) % 2000 + 0.90 AS decimal(18,2)),
       CASE WHEN s.value % 50 = 0 THEN 0 ELSE 1 END
FROM GENERATE_SERIES(1, 1000) AS s;

-- 200 mil pedidos, um a cada 5 minutos desde 01/01/2024 (~23 meses).
-- Cada cliente tem exatamente 10 pedidos. Status: ~1% Created, ~1% Confirmed, ~3% Cancelled, ~95% Completed.
INSERT INTO dbo.Pedidos WITH (TABLOCK) (Id, ClienteId, CriadoEm, Status, Total, EnderecoEntrega)
SELECT s.value,
       (s.value % 20000) + 1,
       DATEADD(MINUTE, s.value * 5, CAST('2024-01-01' AS datetime2(3))),
       CASE (s.value + (s.value / 20000) * 37) % 100
            WHEN 0 THEN 'Created'
            WHEN 1 THEN 'Confirmed'
            WHEN 2 THEN 'Cancelled' WHEN 3 THEN 'Cancelled' WHEN 4 THEN 'Cancelled'
            ELSE 'Completed'
       END,
       0,
       CONCAT(N'Rua das Encomendas, ', s.value, N' - Centro - São Paulo/SP - CEP 01000-000 - bloco ',
              s.value % 50, N', apartamento ', s.value % 300, N' - deixar com o porteiro')
FROM GENERATE_SERIES(1, 200000) AS s;

-- 1 a 4 itens por pedido (~500 mil itens), produtos distintos dentro do pedido.
INSERT INTO dbo.ItensPedido WITH (TABLOCK) (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT p.value,
       pr.Id,
       1 + (p.value + k.value) % 5,
       pr.Preco
FROM GENERATE_SERIES(1, 200000) AS p
CROSS JOIN GENERATE_SERIES(0, 3) AS k
JOIN dbo.Produtos AS pr ON pr.Id = ((p.value * 7 + k.value * 131) % 1000) + 1
WHERE k.value <= p.value % 4;
GO

-- Total do pedido = soma dos itens (desnormalização consciente, ver módulo 3.01).
UPDATE p
SET p.Total = i.Total
FROM dbo.Pedidos AS p
JOIN (SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
      FROM dbo.ItensPedido
      GROUP BY PedidoId) AS i ON i.PedidoId = p.Id;
GO

-- Estatísticas completas: o otimizador decide com números exatos, e o plano fica estável.
UPDATE STATISTICS dbo.Clientes WITH FULLSCAN;
UPDATE STATISTICS dbo.Produtos WITH FULLSCAN;
UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;
CHECKPOINT;
GO
