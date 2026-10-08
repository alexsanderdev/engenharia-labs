using F5M01.Api.Contratos;
using F5M01.Api.Dominio;

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
    public static Dictionary<string, string[]> Validar(ConsultaPedidosQuery query) =>
        throw new NotImplementedException("TODO (passo 5): valide status, ordenarPor (whitelist), pagina e tamanhoPagina.");

    /// <summary>
    /// Executa uma consulta JÁ VALIDADA: filtra (status, clienteId), ordena (desempate por Id para a ordem ser estável),
    /// pagina e monta metadados e links. Os links são relativos a <paramref name="caminhoBase"/> (ex.: <c>/pedidos</c>)
    /// e preservam os filtros e a ordenação informados, sempre com <c>pagina</c> e <c>tamanhoPagina</c>.
    /// <c>TotalPaginas</c> = teto(total / tamanho); <c>Last</c> aponta para max(1, TotalPaginas);
    /// <c>Prev</c> é null na página 1; <c>Next</c> é null a partir da última página.
    /// </summary>
    public static PaginaResponse<PedidoResumoResponse> Executar(IEnumerable<Pedido> pedidos, ConsultaPedidosQuery query, string caminhoBase) =>
        throw new NotImplementedException(
            "TODO (passo 5): filtre, ordene (ThenBy Id), pagine (Skip/Take) e monte os links com QueryHelpers.AddQueryString.");
}
