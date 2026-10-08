namespace F4M07.Patterns.Checkout;

// Passo 6 — cada elo valida UMA coisa. Válido? return proxima(); inválido? return ResultadoDaValidacao.Invalido(...)
// SEM chamar proxima() (isso interrompe a corrente).

/// <summary>Carrinho sem itens → "carrinho.vazio".</summary>
public sealed class CarrinhoNaoVazio : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct) =>
        throw new NotImplementedException("TODO: Itens.Count == 0 → Invalido(\"carrinho.vazio\", ...); senão proxima().");
}

/// <summary>Algum item com quantidade &lt;= 0 → "item.quantidade_invalida" (mensagem cita o SKU).</summary>
public sealed class QuantidadesPositivas : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct) =>
        throw new NotImplementedException("TODO: primeiro item com Quantidade <= 0 → Invalido(\"item.quantidade_invalida\", mensagem com o SKU).");
}

/// <summary>SKU inexistente → "produto.inexistente"; produto inativo → "produto.inativo" (mensagem cita o SKU).</summary>
public sealed class ProdutosAtivos(ICatalogoParaCheckout catalogo) : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct) =>
        throw new NotImplementedException($"TODO: para cada item, await catalogo.ObterAsync(sku, ct): null → inexistente; !Ativo → inativo. ({catalogo.GetType().Name})");
}

/// <summary>
/// Soma das quantidades do MESMO SKU (pode aparecer em mais de uma linha) maior que o estoque → "estoque.insuficiente".
/// Assume que <see cref="ProdutosAtivos"/> já rodou antes (por isso a ORDEM da corrente importa).
/// </summary>
public sealed class EstoqueSuficiente(ICatalogoParaCheckout catalogo) : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct) =>
        throw new NotImplementedException($"TODO: agrupe por SKU (OrdinalIgnoreCase), some as quantidades e compare com o Estoque. ({catalogo.GetType().Name})");
}
