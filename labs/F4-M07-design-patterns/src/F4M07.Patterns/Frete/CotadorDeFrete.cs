namespace F4M07.Patterns.Frete;

/// <summary>
/// CONTEXTO do Strategy: recebe TODAS as estratégias (vindas do container) e escolhe pela modalidade.
/// Não tem <c>switch</c>: modalidade nova = classe nova registrada no DI, sem editar esta classe (OCP).
/// </summary>
public sealed class CotadorDeFrete : ICotadorDeFrete
{
    private readonly Dictionary<string, ICalculadoraDeFrete> _porModalidade;

    /// <summary>
    /// Indexa as estratégias por <see cref="ICalculadoraDeFrete.Modalidade"/> (sem diferenciar maiúsculas).
    /// Duas estratégias com a mesma modalidade é erro de composição: lance <see cref="InvalidOperationException"/>.
    /// </summary>
    public CotadorDeFrete(IEnumerable<ICalculadoraDeFrete> calculadoras)
    {
        _porModalidade = new Dictionary<string, ICalculadoraDeFrete>(StringComparer.OrdinalIgnoreCase);
        foreach (var calculadora in calculadoras)
        {
            if (!_porModalidade.TryAdd(calculadora.Modalidade, calculadora))
                throw new InvalidOperationException($"Modalidade de frete '{calculadora.Modalidade}' registrada mais de uma vez.");
        }
    }

    public CotacaoDeFrete Cotar(string modalidade, PedidoParaFrete pedido)
    {
        if (!_porModalidade.TryGetValue(modalidade, out var calculadora))
            throw new ModalidadeDeFreteDesconhecidaException(modalidade, _porModalidade.Keys.Order());

        return calculadora.Calcular(pedido);
    }

    public IReadOnlyList<CotacaoDeFrete> CotarTodas(PedidoParaFrete pedido) =>
        [.. _porModalidade.Values
            .Select(c => c.Calcular(pedido))
            .OrderBy(c => c.Valor)
            .ThenBy(c => c.PrazoEmDiasUteis)];
}
