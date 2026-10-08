namespace F4M07.Patterns.Checkout;

/// <summary>
/// Monta a corrente a partir das validações registradas no DI, NA ORDEM DE REGISTRO, e executa.
/// É o mesmo desenho do pipeline de middlewares do ASP.NET Core (cada elo recebe o "next").
/// </summary>
public sealed class PipelineDeValidacaoDoCheckout(IEnumerable<IValidacaoDeCheckout> validacoes)
{
    private readonly IValidacaoDeCheckout[] _validacoes = [.. validacoes];

    /// <summary>
    /// Executa a primeira validação passando um <see cref="ProximaValidacao"/> que chama a segunda, e assim por diante.
    /// O final da corrente devolve <see cref="ResultadoDaValidacao.Valido"/>. Sem validações registradas: válido.
    /// Um elo que não chama <c>proxima()</c> interrompe a corrente (os seguintes NÃO executam).
    /// Antes de cada elo, respeite o cancelamento (<c>ct.ThrowIfCancellationRequested()</c>).
    /// </summary>
    public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, CancellationToken ct = default) =>
        throw new NotImplementedException(
            $"TODO: comece com um ProximaValidacao terminal que devolve Valido e embrulhe de trás para frente as {_validacoes.Length} validações.");
}
