-- =============================================================================
-- Massa de dados da Parte B (PRONTA — não altere).
-- Banco F3M01Consultas, separado do banco da Parte A: você pode fazer as consultas
-- mesmo antes de terminar o seu DDL. O schema aqui é simplificado (sem CHECK/DEFAULT):
-- as constraints são o exercício da Parte A.
-- =============================================================================
CREATE TABLE dbo.Clientes
(
    Id            int           NOT NULL PRIMARY KEY,
    Nome          nvarchar(150) NOT NULL,
    Email         nvarchar(254) NOT NULL,
    IndicadoPorId int           NULL REFERENCES dbo.Clientes (Id),
    CriadoEm      datetime2(3)  NOT NULL
);

CREATE TABLE dbo.Produtos
(
    Id    int           NOT NULL PRIMARY KEY,
    Sku   varchar(20)   NOT NULL,
    Nome  nvarchar(200) NOT NULL,
    Preco decimal(18,2) NOT NULL,
    Ativo bit           NOT NULL
);

CREATE TABLE dbo.Pedidos
(
    Id        int           NOT NULL PRIMARY KEY,
    ClienteId int           NOT NULL REFERENCES dbo.Clientes (Id),
    CriadoEm  datetime2(3)  NOT NULL,
    Status    varchar(20)   NOT NULL,
    Total     decimal(18,2) NOT NULL
);

CREATE TABLE dbo.ItensPedido
(
    PedidoId      int           NOT NULL REFERENCES dbo.Pedidos (Id),
    ProdutoId     int           NOT NULL REFERENCES dbo.Produtos (Id),
    Quantidade    int           NOT NULL,
    PrecoUnitario decimal(18,2) NOT NULL,
    PRIMARY KEY (PedidoId, ProdutoId)
);
GO

INSERT INTO dbo.Clientes (Id, Nome, Email, IndicadoPorId, CriadoEm) VALUES
(1, N'Ana Souza',      N'ana@orderflow.dev',      NULL, '2025-11-02T10:00:00'),
(2, N'Bruno Lima',     N'bruno@orderflow.dev',    1,    '2025-11-10T09:30:00'),
(3, N'Carla Dias',     N'carla@orderflow.dev',    1,    '2025-12-01T14:00:00'),
(4, N'Diego Rocha',    N'diego@orderflow.dev',    2,    '2025-12-15T08:45:00'),
(5, N'Elisa Martins',  N'elisa@orderflow.dev',    NULL, '2026-01-03T11:20:00'),
(6, N'Fábio Nunes',    N'fabio@orderflow.dev',    5,    '2026-01-18T16:10:00'),
(7, N'Gabriela Alves', N'gabriela@orderflow.dev', NULL, '2026-02-07T19:00:00'),
(8, N'Heitor Costa',   N'heitor@orderflow.dev',   3,    '2026-03-12T07:55:00');

INSERT INTO dbo.Produtos (Id, Sku, Nome, Preco, Ativo) VALUES
(1, 'TEC-001', N'Teclado mecânico',    350.00,  1),
(2, 'MOU-001', N'Mouse sem fio',       120.00,  1),
(3, 'MON-001', N'Monitor 27"',         1800.00, 1),
(4, 'CAB-001', N'Cabo HDMI 2 m',       40.00,   1),
(5, 'HEA-001', N'Headset com microfone', 450.00, 1),
(6, 'WEB-001', N'Webcam Full HD',      300.00,  1),
(7, 'SUP-001', N'Suporte de monitor',  180.00,  1),
(8, 'HUB-001', N'Hub USB-C',           210.00,  0);

INSERT INTO dbo.Pedidos (Id, ClienteId, CriadoEm, Status, Total) VALUES
(1,  1, '2026-01-05T10:00:00', 'Completed', 560.00),
(2,  1, '2026-01-20T14:30:00', 'Completed', 1800.00),
(3,  1, '2026-02-11T09:15:00', 'Cancelled', 450.00),
(4,  1, '2026-03-02T16:00:00', 'Completed', 120.00),
(5,  2, '2026-01-07T11:00:00', 'Completed', 200.00),
(6,  2, '2026-02-14T19:45:00', 'Completed', 300.00),
(7,  2, '2026-02-14T19:45:00', 'Confirmed', 450.00),
(8,  3, '2026-01-15T08:30:00', 'Completed', 3600.00),
(9,  3, '2026-03-10T13:00:00', 'Created',   800.00),
(10, 4, '2026-02-01T10:10:00', 'Completed', 200.00),
(11, 4, '2026-02-20T17:00:00', 'Cancelled', 1800.00),
(12, 4, '2026-03-15T12:00:00', 'Completed', 420.00),
(13, 4, '2026-03-28T18:20:00', 'Completed', 350.00),
(14, 5, '2026-03-05T09:00:00', 'Completed', 940.00),
(15, 6, '2026-01-25T15:00:00', 'Completed', 360.00),
(16, 5, '2026-03-31T21:00:00', 'Created',   40.00);

-- Repare no pedido 1: o teclado foi vendido em promoção (320,00), abaixo do preço atual do cadastro.
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES
(1,  1, 1, 320.00), (1,  2, 2, 120.00),
(2,  3, 1, 1800.00),
(3,  5, 1, 450.00),
(4,  4, 3, 40.00),
(5,  2, 1, 120.00), (5,  4, 2, 40.00),
(6,  6, 1, 300.00),
(7,  5, 1, 450.00),
(8,  3, 2, 1800.00),
(9,  1, 1, 350.00), (9,  5, 1, 450.00),
(10, 4, 5, 40.00),
(11, 3, 1, 1800.00),
(12, 6, 1, 300.00), (12, 2, 1, 120.00),
(13, 1, 1, 350.00),
(14, 5, 2, 450.00), (14, 4, 1, 40.00),
(15, 2, 3, 120.00),
(16, 4, 1, 40.00);
GO
