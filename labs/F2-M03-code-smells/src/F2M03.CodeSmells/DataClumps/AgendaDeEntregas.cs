namespace F2M03.CodeSmells.DataClumps;

/// <summary>
/// SMELLS (antes): Long Parameter List + Data Clumps. Os métodos legados recebem 6 a 9 parâmetros primitivos,
/// e o mesmo grupo (endereço; início/fim) se repete. Eles agora são fachadas <see cref="ObsoleteAttribute">obsoletas</see>
/// que montam os objetos novos e delegam.
/// </summary>
public sealed class AgendaDeEntregas
{
    /// <summary>Prazo em dias: expressa = 1; SP = 2; demais UFs = 5.</summary>
    public int EstimarPrazoEmDias(Endereco endereco, bool expressa)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        if (expressa) return 1;
        return endereco.Uf == "SP" ? 2 : 5;
    }

    /// <summary>
    /// Agenda a entrega: data prevista = início da janela + prazo. Se cair depois do fim da janela,
    /// lança <see cref="InvalidOperationException"/>. Etiqueta: "Cliente — linha do endereço" (+ " [EXPRESSA]").
    /// </summary>
    public Agendamento Agendar(SolicitacaoDeEntrega solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);

        var data = solicitacao.Janela.Inicio.AddDays(EstimarPrazoEmDias(solicitacao.Endereco, solicitacao.Expressa));
        if (!solicitacao.Janela.Contem(data))
            throw new InvalidOperationException("O prazo de entrega não cabe na janela pedida.");

        var etiqueta = $"{solicitacao.Cliente} — {solicitacao.Endereco.Linha}" + (solicitacao.Expressa ? " [EXPRESSA]" : "");
        return new Agendamento(data, etiqueta);
    }

    /// <summary>LEGADO: mantido para os chamadores antigos.</summary>
    [Obsolete("Use EstimarPrazoEmDias(Endereco, bool).")]
    public int EstimarPrazoEmDias(string logradouro, string numero, string cidade, string uf, string cep, bool expressa) =>
        EstimarPrazoEmDias(new Endereco(logradouro, numero, cidade, uf, cep), expressa);

    /// <summary>LEGADO: mantido para os chamadores antigos.</summary>
    [Obsolete("Use Agendar(SolicitacaoDeEntrega).")]
    public Agendamento Agendar(
        string cliente, string logradouro, string numero, string cidade, string uf, string cep,
        DateOnly janelaInicio, DateOnly janelaFim, bool expressa) =>
        Agendar(new SolicitacaoDeEntrega(
            cliente,
            new Endereco(logradouro, numero, cidade, uf, cep),
            new JanelaDeEntrega(janelaInicio, janelaFim),
            expressa));
}
