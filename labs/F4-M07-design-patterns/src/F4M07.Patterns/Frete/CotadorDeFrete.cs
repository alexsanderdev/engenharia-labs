namespace F4M07.Patterns.Frete;

/// <summary>
/// CONTEXTO do Strategy: recebe TODAS as estratégias (vindas do container) e escolhe pela modalidade.
/// Não tem <c>switch</c>: modalidade nova = classe nova registrada no DI, sem editar esta classe (OCP).
/// </summary>
public sealed class CotadorDeFrete : ICotadorDeFrete
{
    /// <summary>
    /// Indexa as estratégias por <see cref="ICalculadoraDeFrete.Modalidade"/> (sem diferenciar maiúsculas).
    /// Duas estratégias com a mesma modalidade é erro de composição: lance <see cref="InvalidOperationException"/>.
    /// </summary>
    public CotadorDeFrete(IEnumerable<ICalculadoraDeFrete> calculadoras)
    {
        ArgumentNullException.ThrowIfNull(calculadoras);
        throw new NotImplementedException(
            "TODO: monte um Dictionary<string, ICalculadoraDeFrete>(StringComparer.OrdinalIgnoreCase); TryAdd falhou = InvalidOperationException.");
    }

    /// <inheritdoc />
    public CotacaoDeFrete Cotar(string modalidade, PedidoParaFrete pedido) =>
        throw new NotImplementedException(
            "TODO: ache a estratégia no dicionário e chame Calcular; não achou = ModalidadeDeFreteDesconhecidaException com as chaves disponíveis.");

    /// <inheritdoc />
    public IReadOnlyList<CotacaoDeFrete> CotarTodas(PedidoParaFrete pedido) =>
        throw new NotImplementedException("TODO: calcule com todas as estratégias e ordene por Valor, depois por PrazoEmDiasUteis.");
}
