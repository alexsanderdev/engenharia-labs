namespace F2M03.CodeSmells.DataClumps;

/// <summary>
/// SMELLS: Long Parameter List + Data Clumps. Os métodos legados recebem 6 a 9 parâmetros primitivos,
/// e o mesmo grupo (endereço; início/fim) se repete nas assinaturas e nas validações.
/// Refatoração: Introduce Parameter Object (<see cref="SolicitacaoDeEntrega"/>), tipos para os grupos
/// (<see cref="Endereco"/>, <see cref="JanelaDeEntrega"/>) e os métodos antigos viram fachadas [Obsolete].
/// </summary>
public sealed class AgendaDeEntregas
{
    /// <summary>Prazo em dias: expressa = 1; SP = 2; demais UFs = 5.</summary>
    public int EstimarPrazoEmDias(Endereco endereco, bool expressa) =>
        throw new NotImplementedException("TODO: mesma regra do método legado, recebendo o Endereco.");

    /// <summary>
    /// Agenda a entrega: data prevista = início da janela + prazo. Se cair depois do fim da janela,
    /// lança <see cref="InvalidOperationException"/>. Etiqueta: "Cliente — linha do endereço" (+ " [EXPRESSA]").
    /// </summary>
    public Agendamento Agendar(SolicitacaoDeEntrega solicitacao) =>
        throw new NotImplementedException("TODO: mova a lógica do Agendar legado para cá e faça o legado delegar.");

    /// <summary>LEGADO: prazo em dias. Mesmo grupo de 5 parâmetros de endereço do Agendar.</summary>
    public int EstimarPrazoEmDias(string logradouro, string numero, string cidade, string uf, string cep, bool expressa)
    {
        var ufNormalizada = (uf ?? string.Empty).Trim().ToUpperInvariant();
        if (ufNormalizada.Length != 2 || !ufNormalizada.All(char.IsAsciiLetter))
            throw new ArgumentException("UF deve ter 2 letras.", nameof(uf));

        var digitosCep = new string((cep ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digitosCep.Length != 8)
            throw new ArgumentException("CEP deve ter 8 dígitos.", nameof(cep));

        if (expressa) return 1;
        return ufNormalizada == "SP" ? 2 : 5;
    }

    /// <summary>LEGADO: agenda a entrega com 9 parâmetros.</summary>
    public Agendamento Agendar(
        string cliente, string logradouro, string numero, string cidade, string uf, string cep,
        DateOnly janelaInicio, DateOnly janelaFim, bool expressa)
    {
        // Validação de endereço duplicada de EstimarPrazoEmDias
        var ufNormalizada = (uf ?? string.Empty).Trim().ToUpperInvariant();
        if (ufNormalizada.Length != 2 || !ufNormalizada.All(char.IsAsciiLetter))
            throw new ArgumentException("UF deve ter 2 letras.", nameof(uf));

        var digitosCep = new string((cep ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digitosCep.Length != 8)
            throw new ArgumentException("CEP deve ter 8 dígitos.", nameof(cep));

        if (janelaFim < janelaInicio)
            throw new ArgumentException("A janela termina antes de começar.", nameof(janelaFim));

        var prazo = EstimarPrazoEmDias(logradouro, numero, cidade, ufNormalizada, digitosCep, expressa);
        var data = janelaInicio.AddDays(prazo);
        if (data > janelaFim)
            throw new InvalidOperationException("O prazo de entrega não cabe na janela pedida.");

        var cepFormatado = $"{digitosCep[..5]}-{digitosCep[5..]}";
        var etiqueta = $"{cliente} — {logradouro}, {numero} — {cidade}/{ufNormalizada} — CEP {cepFormatado}";
        if (expressa)
            etiqueta += " [EXPRESSA]";

        return new Agendamento(data, etiqueta);
    }
}
