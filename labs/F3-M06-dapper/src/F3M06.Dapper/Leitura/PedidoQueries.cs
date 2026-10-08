using Dapper;
using Microsoft.Data.SqlClient;

namespace F3M06.Dapper.Leitura;

/// <summary>
/// Read models de pedidos com Dapper: multi-mapping, QueryMultiple, relatório agregado e paginação.
/// </summary>
public sealed class PedidoQueries(string connectionString)
{
    /// <summary>Tamanho máximo de página aceito (protege o banco de "?tamanho=1000000").</summary>
    public const int TamanhoMaximoPagina = 100;

    // TODO: declare aqui as constantes de SQL dos Passos 4 a 8.

    /// <summary>
    /// Passo 4: um pedido com seus itens em UMA consulta (JOIN) usando multi-mapping com <c>splitOn: "ItemId"</c>.
    /// Pedido sem itens vem com <see cref="PedidoDetalhe.Itens"/> vazia. Inexistente devolve <c>null</c>.
    /// Itens ordenados por <c>ItemId</c>.
    /// </summary>
    public Task<PedidoDetalhe?> ObterComItensAsync(Guid pedidoId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 4: SELECT p.Id, p.ClienteId, c.Nome AS ClienteNome, p.CriadoEm, p.Status, p.Total, " +
            "i.Id AS ItemId, i.ProdutoId, pr.Sku, pr.Nome AS NomeProduto, i.Quantidade, i.PrecoUnitario " +
            "FROM Pedidos p JOIN Clientes c ... LEFT JOIN ItensPedido i ... LEFT JOIN Produtos pr ...; " +
            "QueryAsync<PedidoDetalhe, ItemDetalhe?, PedidoDetalhe>(..., splitOn: \"ItemId\").");

    /// <summary>
    /// Passo 5: todos os pedidos do cliente com itens, do mais recente para o mais antigo.
    /// O JOIN repete o cabeçalho em cada linha de item: cada pedido deve aparecer UMA vez.
    /// </summary>
    public Task<IReadOnlyList<PedidoDetalhe>> ListarDoClienteComItensAsync(Guid clienteId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 5: mesmo SELECT do Passo 4 com WHERE p.ClienteId = @ClienteId ORDER BY p.CriadoEm DESC, p.Id DESC, i.Id. " +
            "Use um Dictionary<Guid, PedidoDetalhe> no delegate do multi-mapping para não duplicar o cabeçalho.");

    /// <summary>
    /// Passo 6: painel do cliente com <c>QueryMultiple</c> — três SELECTs, uma ida ao banco:
    /// (1) dados do cliente, (2) os <paramref name="ultimos"/> pedidos mais recentes (inclusive cancelados),
    /// (3) estatísticas sem cancelados. Cliente inexistente devolve <c>null</c>.
    /// </summary>
    public Task<PainelDoCliente?> ObterPainelDoClienteAsync(Guid clienteId, int ultimos = 3, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 6: um SQL com 3 SELECTs separados por ';' e QueryMultipleAsync; leia NA ORDEM com " +
            "ReadSingleOrDefaultAsync<ClienteResumo>, ReadAsync<PedidoResumo> e ReadSingleAsync<EstatisticasDoCliente>. " +
            "Estatísticas: COUNT(*), ISNULL(SUM(Total), 0), MAX(CriadoEm) com Status <> 'Cancelled'.");

    /// <summary>
    /// Passo 7: relatório de vendas por dia entre <paramref name="inicio"/> e <paramref name="fim"/> (dias INCLUSIVOS).
    /// O SQL fica em <c>Sql/RelatorioDeVendas.sql</c> e recebe <c>@Inicio</c> e <c>@FimExclusivo</c>
    /// (meia-noite do dia seguinte a <paramref name="fim"/>). Este método já está pronto: o TODO está no .sql.
    /// </summary>
    public async Task<IReadOnlyList<VendasPorDia>> RelatorioDeVendasAsync(DateOnly inicio, DateOnly fim, CancellationToken ct = default)
    {
        if (fim < inicio)
            throw new ArgumentException("O fim do período não pode ser anterior ao início.", nameof(fim));

        var parametros = new
        {
            Inicio = inicio.ToDateTime(TimeOnly.MinValue),
            FimExclusivo = fim.AddDays(1).ToDateTime(TimeOnly.MinValue),
        };

        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlArquivos.Ler("RelatorioDeVendas.sql"), parametros, cancellationToken: ct);
        var linhas = await conexao.QueryAsync<VendasPorDia>(comando);
        return linhas.AsList();
    }

    /// <summary>
    /// Passo 8: paginação por OFFSET/FETCH de todos os pedidos (mais recentes primeiro, desempate por Id),
    /// com o total de itens para a UI. <paramref name="numeroPagina"/> começa em 1;
    /// <paramref name="tamanho"/> entre 1 e <see cref="TamanhoMaximoPagina"/>. Fora disso: <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public Task<Pagina<PedidoResumo>> ListarPaginadoAsync(int numeroPagina, int tamanho, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 8: valide os argumentos (ArgumentOutOfRangeException.ThrowIfLessThan/ThrowIfGreaterThan); " +
            "SELECT COUNT(*) FROM Pedidos; + SELECT ... ORDER BY p.CriadoEm DESC, p.Id DESC OFFSET @Pular ROWS FETCH NEXT @Tamanho ROWS ONLY; " +
            "num QueryMultipleAsync.");
}
