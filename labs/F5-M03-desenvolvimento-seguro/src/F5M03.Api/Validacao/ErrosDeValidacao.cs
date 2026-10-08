namespace F5M03.Api.Validacao;

/// <summary>
/// ARQUIVO PRONTO — ajudante para acumular erros de validação por campo e devolvê-los
/// no formato de <c>TypedResults.ValidationProblem(...)</c> (HTTP 400, application/problem+json).
/// </summary>
public sealed class ErrosDeValidacao
{
    private readonly Dictionary<string, List<string>> _erros = new(StringComparer.Ordinal);

    public bool Vazio => _erros.Count == 0;

    public ErrosDeValidacao Adicionar(string campo, string mensagem)
    {
        if (!_erros.TryGetValue(campo, out var lista))
            _erros[campo] = lista = [];
        lista.Add(mensagem);
        return this;
    }

    /// <summary>Adiciona o erro quando a condição de falha é verdadeira.</summary>
    public ErrosDeValidacao Se(bool falhou, string campo, string mensagem) =>
        falhou ? Adicionar(campo, mensagem) : this;

    public Dictionary<string, string[]> ParaDicionario() =>
        _erros.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal);
}
