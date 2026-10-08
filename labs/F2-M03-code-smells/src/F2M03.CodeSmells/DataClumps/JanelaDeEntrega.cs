namespace F2M03.CodeSmells.DataClumps;

/// <summary>
/// O par (início, fim) que andava sempre junto vira um tipo com a invariante "fim ≥ início".
/// </summary>
public readonly record struct JanelaDeEntrega
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    /// <exception cref="ArgumentException">Fim antes do início.</exception>
    public JanelaDeEntrega(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
            throw new ArgumentException("A janela termina antes de começar.", nameof(fim));

        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>Verdadeiro se a data está dentro da janela (inclusive nas pontas).</summary>
    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
}

/// <summary>
/// Parameter object: substitui a lista longa de parâmetros de <c>Agendar</c>.
/// </summary>
public sealed record SolicitacaoDeEntrega(string Cliente, Endereco Endereco, JanelaDeEntrega Janela, bool Expressa);

/// <summary>Resultado do agendamento.</summary>
public sealed record Agendamento(DateOnly DataPrevista, string Etiqueta);
