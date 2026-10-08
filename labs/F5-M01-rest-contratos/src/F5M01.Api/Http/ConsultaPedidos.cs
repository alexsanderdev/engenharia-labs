using F5M01.Api.Contratos;
using F5M01.Api.Dominio;
using Microsoft.AspNetCore.WebUtilities;

namespace F5M01.Api.Http;

/// <summary>
/// Parâmetros de <c>GET /pedidos</c> (query string). Exemplo:
/// <c>/pedidos?status=Confirmed&amp;clienteId=...&amp;ordenarPor=-total&amp;pagina=2&amp;tamanhoPagina=10</c>.
/// <c>status</c> chega como texto para a API devolver ValidationProblem (e não um 400 genérico de binding) quando for inválido.
/// </summary>
public sealed record ConsultaPedidosQuery(string? Status, Guid? ClienteId, string? OrdenarPor, int? Pagina, int? TamanhoPagina);

/// <summary>Filtro, ordenação e paginação de pedidos.</summary>
public static class ConsultaPedidos
{
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    /// <summary>Campos ordenáveis (whitelist). Prefixo <c>-</c> = decrescente. Padrão: <c>criadoEm</c> crescente.</summary>
    public static readonly IReadOnlyList<string> CamposOrdenaveis = ["criadoEm", "total"];

    /// <summary>
    /// Valida a consulta. Devolve um dicionário vazio quando está tudo certo; senão, os erros por parâmetro
    /// (chaves em camelCase: <c>status</c>, <c>ordenarPor</c>, <c>pagina</c>, <c>tamanhoPagina</c>).
    /// Regras: status deve ser um <see cref="StatusPedido"/> (sem diferenciar maiúsculas); ordenarPor só da whitelist
    /// (com ou sem <c>-</c>); pagina ≥ 1; 1 ≤ tamanhoPagina ≤ <see cref="TamanhoMaximo"/>.
    /// </summary>
    public static Dictionary<string, string[]> Validar(ConsultaPedidosQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var erros = new Dictionary<string, string[]>();

        if (query.Status is not null && !Enum.TryParse<StatusPedido>(query.Status, ignoreCase: true, out _))
            erros["status"] = [$"Status inválido. Use: {string.Join(", ", Enum.GetNames<StatusPedido>())}."];

        if (query.OrdenarPor is not null && CampoDeOrdenacao(query.OrdenarPor) is null)
            erros["ordenarPor"] = [$"Ordenação inválida. Use: {string.Join(", ", CamposOrdenaveis)} (prefixo '-' para decrescente)."];

        if (query.Pagina is < 1)
            erros["pagina"] = ["A página começa em 1."];

        if (query.TamanhoPagina is < 1 or > TamanhoMaximo)
            erros["tamanhoPagina"] = [$"O tamanho da página deve estar entre 1 e {TamanhoMaximo}."];

        return erros;
    }

    /// <summary>
    /// Executa uma consulta JÁ VALIDADA: filtra (status, clienteId), ordena (desempate por Id para a ordem ser estável),
    /// pagina e monta metadados e links. Os links são relativos a <paramref name="caminhoBase"/> (ex.: <c>/pedidos</c>)
    /// e preservam os filtros e a ordenação informados, sempre com <c>pagina</c> e <c>tamanhoPagina</c>.
    /// <c>TotalPaginas</c> = teto(total / tamanho); <c>Last</c> aponta para max(1, TotalPaginas);
    /// <c>Prev</c> é null na página 1; <c>Next</c> é null a partir da última página.
    /// </summary>
    public static PaginaResponse<PedidoResumoResponse> Executar(IEnumerable<Pedido> pedidos, ConsultaPedidosQuery query, string caminhoBase)
    {
        ArgumentNullException.ThrowIfNull(pedidos);
        ArgumentNullException.ThrowIfNull(query);

        var filtrados = pedidos;
        if (query.Status is not null)
        {
            var status = Enum.Parse<StatusPedido>(query.Status, ignoreCase: true);
            filtrados = filtrados.Where(p => p.Status == status);
        }
        if (query.ClienteId is { } clienteId)
            filtrados = filtrados.Where(p => p.ClienteId == clienteId);

        var (campo, decrescente) = CampoDeOrdenacao(query.OrdenarPor ?? "criadoEm")!.Value;
        IOrderedEnumerable<Pedido> ordenados = (campo, decrescente) switch
        {
            ("total", false) => filtrados.OrderBy(p => p.Total),
            ("total", true) => filtrados.OrderByDescending(p => p.Total),
            (_, false) => filtrados.OrderBy(p => p.CriadoEm),
            (_, true) => filtrados.OrderByDescending(p => p.CriadoEm),
        };
        var lista = ordenados.ThenBy(p => p.Id).ToList();

        var pagina = query.Pagina ?? 1;
        var tamanho = query.TamanhoPagina ?? TamanhoPadrao;
        var totalItens = lista.Count;
        var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanho);
        var itens = lista.Skip((pagina - 1) * tamanho).Take(tamanho).Select(PedidoResumoResponse.De).ToList();

        string Link(int p)
        {
            var parametros = new Dictionary<string, string?>
            {
                ["pagina"] = p.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["tamanhoPagina"] = tamanho.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            if (query.Status is not null) parametros["status"] = query.Status;
            if (query.ClienteId is not null) parametros["clienteId"] = query.ClienteId.Value.ToString();
            if (query.OrdenarPor is not null) parametros["ordenarPor"] = query.OrdenarPor;
            return QueryHelpers.AddQueryString(caminhoBase, parametros);
        }

        var ultima = Math.Max(1, totalPaginas);
        var links = new LinksPaginacao(
            Self: Link(pagina),
            First: Link(1),
            Prev: pagina > 1 ? Link(Math.Min(pagina - 1, ultima)) : null,
            Next: pagina < totalPaginas ? Link(pagina + 1) : null,
            Last: Link(ultima));

        return new PaginaResponse<PedidoResumoResponse>(itens, pagina, tamanho, totalItens, totalPaginas, links);
    }

    private static (string Campo, bool Decrescente)? CampoDeOrdenacao(string ordenarPor)
    {
        var decrescente = ordenarPor.StartsWith('-');
        var nome = decrescente ? ordenarPor[1..] : ordenarPor;
        var campo = CamposOrdenaveis.FirstOrDefault(c => c.Equals(nome, StringComparison.OrdinalIgnoreCase));
        return campo is null ? null : (campo, decrescente);
    }
}
