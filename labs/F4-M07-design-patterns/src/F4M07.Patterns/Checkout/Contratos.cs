namespace F4M07.Patterns.Checkout;

public sealed record ItemDoCarrinho(string Sku, int Quantidade);

/// <summary>O que chega ao checkout.</summary>
public sealed record ContextoDeCheckout(Guid ClienteId, IReadOnlyList<ItemDoCarrinho> Itens);

/// <summary>Resultado de uma validação: válido, ou inválido com código estável (para a API) e mensagem.</summary>
public sealed record ResultadoDaValidacao(bool EhValido, string? Codigo = null, string? Mensagem = null)
{
    public static ResultadoDaValidacao Valido { get; } = new(true);

    public static ResultadoDaValidacao Invalido(string codigo, string mensagem) => new(false, codigo, mensagem);
}

/// <summary>Chama o próximo elo da corrente.</summary>
public delegate ValueTask<ResultadoDaValidacao> ProximaValidacao();

/// <summary>
/// CHAIN OF RESPONSIBILITY: cada elo valida UMA coisa e decide se passa adiante (<c>await proxima()</c>)
/// ou interrompe devolvendo <see cref="ResultadoDaValidacao.Invalido"/>.
/// </summary>
public interface IValidacaoDeCheckout
{
    ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct);
}

/// <summary>Produto como o checkout enxerga.</summary>
public sealed record ProdutoParaCheckout(string Sku, bool Ativo, int Estoque);

/// <summary>Porta de leitura do catálogo para o checkout.</summary>
public interface ICatalogoParaCheckout
{
    ValueTask<ProdutoParaCheckout?> ObterAsync(string sku, CancellationToken ct);
}

/// <summary>Catálogo em memória. PRONTO — não altere.</summary>
public sealed class CatalogoEmMemoria : ICatalogoParaCheckout
{
    private readonly Dictionary<string, ProdutoParaCheckout> _produtos = new(StringComparer.OrdinalIgnoreCase);

    public CatalogoEmMemoria Com(string sku, bool ativo = true, int estoque = 10)
    {
        _produtos[sku] = new ProdutoParaCheckout(sku, ativo, estoque);
        return this;
    }

    public ValueTask<ProdutoParaCheckout?> ObterAsync(string sku, CancellationToken ct) =>
        ValueTask.FromResult(_produtos.GetValueOrDefault(sku));
}

/// <summary>Opções do checkout (seção "Checkout").</summary>
public sealed class OpcoesDeCheckout
{
    /// <summary>Percentual da taxa de serviço sobre o subtotal. 0,02 = 2%.</summary>
    public decimal PercentualDeTaxaDeServico { get; set; } = 0.02m;
}
