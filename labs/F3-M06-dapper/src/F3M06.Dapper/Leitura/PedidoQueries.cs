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

    // Colunas do pedido ANTES de "ItemId"; colunas do item a partir de "ItemId" (é ali que o splitOn corta).
    private const string SqlPedidoComItens = """
        SELECT p.Id, p.ClienteId, c.Nome AS ClienteNome, p.CriadoEm, p.Status, p.Total,
               i.Id AS ItemId, i.ProdutoId, pr.Sku, pr.Nome AS NomeProduto, i.Quantidade, i.PrecoUnitario
        FROM Pedidos AS p
        INNER JOIN Clientes AS c ON c.Id = p.ClienteId
        LEFT JOIN ItensPedido AS i ON i.PedidoId = p.Id
        LEFT JOIN Produtos AS pr ON pr.Id = i.ProdutoId
        """;

    private const string SqlPainel = """
        SELECT Id, Nome, Email FROM Clientes WHERE Id = @ClienteId;

        SELECT TOP (@Ultimos) p.Id, c.Nome AS ClienteNome, p.CriadoEm, p.Status, p.Total
        FROM Pedidos AS p
        INNER JOIN Clientes AS c ON c.Id = p.ClienteId
        WHERE p.ClienteId = @ClienteId
        ORDER BY p.CriadoEm DESC, p.Id DESC;

        SELECT COUNT(*) AS QuantidadePedidos,
               ISNULL(SUM(p.Total), 0) AS TotalGasto,
               MAX(p.CriadoEm) AS UltimoPedidoEm
        FROM Pedidos AS p
        WHERE p.ClienteId = @ClienteId AND p.Status <> 'Cancelled';
        """;

    private const string SqlPaginado = """
        SELECT COUNT(*) FROM Pedidos;

        SELECT p.Id, c.Nome AS ClienteNome, p.CriadoEm, p.Status, p.Total
        FROM Pedidos AS p
        INNER JOIN Clientes AS c ON c.Id = p.ClienteId
        ORDER BY p.CriadoEm DESC, p.Id DESC
        OFFSET @Pular ROWS FETCH NEXT @Tamanho ROWS ONLY;
        """;

    /// <summary>
    /// Passo 4: um pedido com seus itens em UMA consulta (JOIN) usando multi-mapping com <c>splitOn: "ItemId"</c>.
    /// Pedido sem itens vem com <see cref="PedidoDetalhe.Itens"/> vazia. Inexistente devolve <c>null</c>.
    /// Itens ordenados por <c>ItemId</c>.
    /// </summary>
    public async Task<PedidoDetalhe?> ObterComItensAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var sql = SqlPedidoComItens + " WHERE p.Id = @PedidoId ORDER BY i.Id;";
        var pedidos = await ConsultarPedidosComItensAsync(sql, new { PedidoId = pedidoId }, ct);
        return pedidos.SingleOrDefault();
    }

    /// <summary>
    /// Passo 5: todos os pedidos do cliente com itens, do mais recente para o mais antigo.
    /// O JOIN repete o cabeçalho em cada linha de item: cada pedido deve aparecer UMA vez.
    /// </summary>
    public async Task<IReadOnlyList<PedidoDetalhe>> ListarDoClienteComItensAsync(Guid clienteId, CancellationToken ct = default)
    {
        var sql = SqlPedidoComItens + " WHERE p.ClienteId = @ClienteId ORDER BY p.CriadoEm DESC, p.Id DESC, i.Id;";
        return await ConsultarPedidosComItensAsync(sql, new { ClienteId = clienteId }, ct);
    }

    private async Task<IReadOnlyList<PedidoDetalhe>> ConsultarPedidosComItensAsync(string sql, object parametros, CancellationToken ct)
    {
        // Dicionário "identity map": a primeira linha de cada pedido cria o objeto; as seguintes só adicionam itens.
        var porId = new Dictionary<Guid, PedidoDetalhe>();
        var ordem = new List<PedidoDetalhe>();

        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(sql, parametros, cancellationToken: ct);
        await conexao.QueryAsync<PedidoDetalhe, ItemDetalhe?, PedidoDetalhe>(
            comando,
            (pedidoDaLinha, item) =>
            {
                if (!porId.TryGetValue(pedidoDaLinha.Id, out var pedido))
                {
                    pedido = pedidoDaLinha;
                    porId.Add(pedido.Id, pedido);
                    ordem.Add(pedido);
                }

                // LEFT JOIN sem item: todas as colunas a partir do split são NULL e o Dapper entrega null.
                if (item is not null)
                    pedido.Itens.Add(item);

                return pedido;
            },
            splitOn: "ItemId");

        return ordem;
    }

    /// <summary>
    /// Passo 6: painel do cliente com <c>QueryMultiple</c> — três SELECTs, uma ida ao banco:
    /// (1) dados do cliente, (2) os <paramref name="ultimos"/> pedidos mais recentes (inclusive cancelados),
    /// (3) estatísticas sem cancelados. Cliente inexistente devolve <c>null</c>.
    /// </summary>
    public async Task<PainelDoCliente?> ObterPainelDoClienteAsync(Guid clienteId, int ultimos = 3, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlPainel, new { ClienteId = clienteId, Ultimos = ultimos }, cancellationToken: ct);
        await using var resultados = await conexao.QueryMultipleAsync(comando);

        // Os result sets são lidos NA ORDEM em que aparecem no SQL.
        var cliente = await resultados.ReadSingleOrDefaultAsync<ClienteResumo>();
        var pedidos = (await resultados.ReadAsync<PedidoResumo>()).AsList();
        var estatisticas = await resultados.ReadSingleAsync<EstatisticasDoCliente>();

        return cliente is null ? null : new PainelDoCliente(cliente, pedidos, estatisticas);
    }

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
    public async Task<Pagina<PedidoResumo>> ListarPaginadoAsync(int numeroPagina, int tamanho, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroPagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanho, TamanhoMaximoPagina);

        var parametros = new { Pular = (numeroPagina - 1) * tamanho, Tamanho = tamanho };

        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlPaginado, parametros, cancellationToken: ct);
        await using var resultados = await conexao.QueryMultipleAsync(comando);

        var total = await resultados.ReadSingleAsync<int>();
        var itens = (await resultados.ReadAsync<PedidoResumo>()).AsList();
        return new Pagina<PedidoResumo>(itens, numeroPagina, tamanho, total);
    }
}
