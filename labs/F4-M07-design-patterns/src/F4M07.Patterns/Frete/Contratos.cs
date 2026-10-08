namespace F4M07.Patterns.Frete;

/// <summary>Chaves das modalidades de frete (usadas também como chave dos keyed services).</summary>
public static class ModalidadeFrete
{
    public const string Economico = "economico";
    public const string Expresso = "expresso";
    public const string Retirada = "retirada";
}

/// <summary>O que o frete precisa saber do pedido — e nada além disso.</summary>
/// <param name="Subtotal">Soma dos itens, em reais.</param>
/// <param name="PesoKg">Peso total em kg (o frete cobra por kg iniciado).</param>
/// <param name="Uf">UF de destino, ex.: "SP".</param>
public sealed record PedidoParaFrete(decimal Subtotal, decimal PesoKg, string Uf);

/// <summary>Resultado de uma cotação.</summary>
public sealed record CotacaoDeFrete(string Modalidade, decimal Valor, int PrazoEmDiasUteis);

/// <summary>
/// STRATEGY: cada modalidade de frete é um algoritmo intercambiável atrás deste contrato.
/// Contrato (LSP): <c>Valor &gt;= 0</c> e <c>PrazoEmDiasUteis &gt;= 0</c>; <see cref="Modalidade"/> é única.
/// </summary>
public interface ICalculadoraDeFrete
{
    /// <summary>Chave da modalidade (uma das constantes de <see cref="ModalidadeFrete"/> ou nova).</summary>
    string Modalidade { get; }

    CotacaoDeFrete Calcular(PedidoParaFrete pedido);
}

/// <summary>Porta usada pelo checkout: escolhe a estratégia pela modalidade pedida pelo cliente.</summary>
public interface ICotadorDeFrete
{
    /// <summary>Cota uma modalidade específica. Modalidade desconhecida lança <see cref="ModalidadeDeFreteDesconhecidaException"/>.</summary>
    CotacaoDeFrete Cotar(string modalidade, PedidoParaFrete pedido);

    /// <summary>Cota todas as modalidades registradas, da mais barata para a mais cara (empate: menor prazo).</summary>
    IReadOnlyList<CotacaoDeFrete> CotarTodas(PedidoParaFrete pedido);
}

/// <summary>O cliente pediu uma modalidade que não está registrada.</summary>
public sealed class ModalidadeDeFreteDesconhecidaException(string modalidade, IEnumerable<string> disponiveis)
    : Exception($"Modalidade de frete '{modalidade}' desconhecida. Disponíveis: {string.Join(", ", disponiveis)}.")
{
    public string Modalidade { get; } = modalidade;
}
