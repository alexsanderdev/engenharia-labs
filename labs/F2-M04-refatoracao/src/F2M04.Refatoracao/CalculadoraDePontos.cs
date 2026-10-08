namespace F2M04.Refatoracao;

/// <summary>
/// LEGADO do OrderFlow: pontos de fidelidade por compra. Hoje é uma fachada fina: converte as strings do
/// contrato antigo para a API nova (<see cref="ProgramaDeFidelidade"/>) e delega. O contrato antigo
/// (incluindo as esquisitices, como categoria desconhecida valer Bronze) continua idêntico — o teste de
/// aprovação garante.
/// </summary>
public sealed class CalculadoraDePontos
{
    private readonly ProgramaDeFidelidade _programa = new();

    /// <summary>
    /// Calcula os pontos de uma compra.
    /// </summary>
    /// <param name="categoria">"BRONZE", "PRATA" ou "OURO" (sem diferenciar maiúsculas; desconhecida = Bronze).</param>
    /// <param name="valorPedido">Valor total do pedido, em reais.</param>
    /// <param name="formaPagamento">"PIX", "CARTAO" ou "BOLETO" (desconhecida = sem bônus).</param>
    /// <param name="primeiraCompra">Primeira compra do cliente.</param>
    /// <param name="mesDeAniversario">A compra foi feita no mês de aniversário do cliente.</param>
    /// <exception cref="ArgumentOutOfRangeException">Valor negativo.</exception>
    public int Calcular(string categoria, decimal valorPedido, string formaPagamento, bool primeiraCompra, bool mesDeAniversario) =>
        _programa.CalcularPontos(new Compra(
            ConverterCategoria(categoria),
            valorPedido,
            ConverterPagamento(formaPagamento),
            primeiraCompra,
            mesDeAniversario));

    private static Categoria ConverterCategoria(string? categoria) =>
        categoria?.Trim().ToUpperInvariant() switch
        {
            "OURO" => Categoria.Ouro,
            "PRATA" => Categoria.Prata,
            _ => Categoria.Bronze, // comportamento herdado: qualquer outra coisa vale Bronze
        };

    private static FormaDePagamento ConverterPagamento(string? formaPagamento) =>
        formaPagamento?.Trim().ToUpperInvariant() switch
        {
            "PIX" => FormaDePagamento.Pix,
            "CARTAO" => FormaDePagamento.Cartao,
            "BOLETO" => FormaDePagamento.Boleto,
            _ => FormaDePagamento.Outra,
        };
}
