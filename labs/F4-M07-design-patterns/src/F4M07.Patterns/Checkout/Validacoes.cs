namespace F4M07.Patterns.Checkout;

/// <summary>Carrinho sem itens → "carrinho.vazio".</summary>
public sealed class CarrinhoNaoVazio : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct) =>
        contexto.Itens.Count == 0
            ? ValueTask.FromResult(ResultadoDaValidacao.Invalido("carrinho.vazio", "O carrinho está vazio."))
            : proxima();
}

/// <summary>Algum item com quantidade &lt;= 0 → "item.quantidade_invalida" (mensagem cita o SKU).</summary>
public sealed class QuantidadesPositivas : IValidacaoDeCheckout
{
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct)
    {
        var invalido = contexto.Itens.FirstOrDefault(i => i.Quantidade <= 0);
        return invalido is null
            ? proxima()
            : ValueTask.FromResult(ResultadoDaValidacao.Invalido(
                "item.quantidade_invalida", $"Quantidade inválida para {invalido.Sku}: {invalido.Quantidade}."));
    }
}

/// <summary>SKU inexistente → "produto.inexistente"; produto inativo → "produto.inativo" (mensagem cita o SKU).</summary>
public sealed class ProdutosAtivos(ICatalogoParaCheckout catalogo) : IValidacaoDeCheckout
{
    public async ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct)
    {
        foreach (var item in contexto.Itens)
        {
            var produto = await catalogo.ObterAsync(item.Sku, ct);
            if (produto is null)
                return ResultadoDaValidacao.Invalido("produto.inexistente", $"Produto {item.Sku} não existe.");
            if (!produto.Ativo)
                return ResultadoDaValidacao.Invalido("produto.inativo", $"Produto {item.Sku} está inativo.");
        }

        return await proxima();
    }
}

/// <summary>
/// Soma das quantidades do MESMO SKU (pode aparecer em mais de uma linha) maior que o estoque → "estoque.insuficiente".
/// Assume que <see cref="ProdutosAtivos"/> já rodou antes (por isso a ORDEM da corrente importa).
/// </summary>
public sealed class EstoqueSuficiente(ICatalogoParaCheckout catalogo) : IValidacaoDeCheckout
{
    public async ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct)
    {
        var porSku = contexto.Itens
            .GroupBy(i => i.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Sku: g.Key, Quantidade: g.Sum(i => i.Quantidade)));

        foreach (var (sku, quantidade) in porSku)
        {
            var produto = await catalogo.ObterAsync(sku, ct);
            var disponivel = produto?.Estoque ?? 0;
            if (disponivel < quantidade)
                return ResultadoDaValidacao.Invalido(
                    "estoque.insuficiente", $"Estoque insuficiente para {sku}: pedido {quantidade}, disponível {disponivel}.");
        }

        return await proxima();
    }
}
