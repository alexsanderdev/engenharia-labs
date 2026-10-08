-- =============================================================================
-- Indices.sql (SOLUÇÃO) — aplicado uma vez sobre a massa de 200 mil pedidos.
-- Cada índice existe por causa de uma consulta (comentário = justificativa).
-- =============================================================================

-- 01 · Pedidos do cliente: igualdade (ClienteId) primeiro, depois a ordenação (CriadoEm).
--      INCLUDE leva Status e Total para a folha: sem Key Lookup. Também serve de índice da FK.
CREATE INDEX IX_Pedidos_ClienteId_CriadoEm
    ON dbo.Pedidos (ClienteId, CriadoEm DESC)
    INCLUDE (Status, Total);

-- 02 · Vendas do produto: a PK de ItensPedido começa por PedidoId, então ProdutoId (FK) precisa do seu índice.
--      Sem o INCLUDE: Seek + ~600 Key Lookups (~1.850 leituras). Com ele: ~6 leituras.
CREATE INDEX IX_ItensPedido_ProdutoId
    ON dbo.ItensPedido (ProdutoId)
    INCLUDE (Quantidade, PrecoUnitario);

-- 03 · Fila de pedidos em aberto: só ~1% das linhas é 'Created'. Índice filtrado = pequeno e barato de manter.
CREATE INDEX IX_Pedidos_EmAberto
    ON dbo.Pedidos (CriadoEm)
    INCLUDE (ClienteId, Total)
    WHERE Status = 'Created';

-- 04 · Faturamento do mês: intervalo em CriadoEm, cobrindo Total.
CREATE INDEX IX_Pedidos_CriadoEm
    ON dbo.Pedidos (CriadoEm)
    INCLUDE (Total);

-- 05 · Cliente por documento: UQ_Clientes_Documento já existe; o problema era a conversão implícita.
