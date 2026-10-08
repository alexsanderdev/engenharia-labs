-- Relatório de vendas por dia (pedidos não cancelados) no período [@Inicio, @FimExclusivo).
-- Parâmetros: @Inicio (datetime2, meia-noite do 1º dia) e @FimExclusivo (meia-noite do dia seguinte ao último).
-- Colunas esperadas (alias = nome da propriedade em VendasPorDia): Dia, QuantidadePedidos, ItensVendidos, Faturamento.
-- Uma linha por dia que teve venda, ordenadas por Dia.
--
-- Cuidados:
--  * Filtre a COLUNA crua (p.CriadoEm >= @Inicio AND p.CriadoEm < @FimExclusivo): sargável, usa o índice.
--    CAST(p.CriadoEm AS date) BETWEEN ... no WHERE esconde a coluna dentro de uma função.
--  * O JOIN com ItensPedido repete o pedido em cada item: SUM(p.Total) somaria o total várias vezes.
--
-- TODO Passo 7: substitua o placeholder abaixo (ele executa, mas não devolve nenhuma linha).
SELECT
    CAST(NULL AS date)            AS Dia,
    0                             AS QuantidadePedidos,
    0                             AS ItensVendidos,
    CAST(0 AS decimal(18, 2))     AS Faturamento
WHERE 1 = 0
  AND @Inicio < @FimExclusivo;
