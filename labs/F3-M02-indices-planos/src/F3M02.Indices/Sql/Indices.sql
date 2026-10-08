-- =============================================================================
-- Indices.sql — aplicado UMA vez sobre a massa de 200 mil pedidos, antes dos testes.
-- Regra do lab: cada índice existe por causa de uma consulta. Escreva a justificativa
-- num comentário acima de cada CREATE INDEX (qual consulta, qual operador some do plano).
--
-- Tabelas (já existem, com PK clusterizada em Id; ItensPedido em (PedidoId, ProdutoId)):
--   dbo.Clientes   (Id, Nome, Email, Documento varchar(11) UNIQUE, CriadoEm)
--   dbo.Produtos   (Id, Sku, Nome, Preco, Ativo)
--   dbo.Pedidos    (Id, ClienteId, CriadoEm, Status, Total, EnderecoEntrega)
--   dbo.ItensPedido(PedidoId, ProdutoId, Quantidade, PrecoUnitario)
--
-- Limite: no máximo 4 índices não clusterizados em dbo.Pedidos e nenhum redundante.
-- =============================================================================

-- TODO (Passos 1 e 2) · Consultas/01-PedidosDoCliente.sql: Index Seek, sem Key Lookup, sem Sort.

-- TODO (Passo 3) · Consultas/02-VendasDoProduto.sql: índice na FK ItensPedido.ProdutoId, cobrindo a consulta.

-- TODO (Passo 4) · Consultas/03-PedidosEmAberto.sql: índice FILTRADO só com os pedidos 'Created'.

-- TODO (Passo 5) · Consultas/04-FaturamentoDoMes.sql: índice para o intervalo de CriadoEm (e corrija a consulta).

-- Passo 6 · Consultas/05-ClientePorDocumento.sql: o índice único já existe; o problema está na consulta.
