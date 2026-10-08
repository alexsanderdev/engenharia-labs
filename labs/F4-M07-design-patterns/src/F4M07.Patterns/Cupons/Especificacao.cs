namespace F4M07.Patterns.Cupons;

/// <summary>
/// SPECIFICATION: uma regra de negócio booleana com nome, testável sozinha e combinável com E/OU/NÃO.
/// Use os métodos <see cref="E"/>, <see cref="Ou"/>, <see cref="Nao"/> ou os operadores <c>&amp;</c>, <c>|</c>, <c>!</c>.
/// </summary>
public abstract class Especificacao<T>
{
    public abstract bool EhSatisfeitaPor(T candidato);

    /// <summary>Texto legível da regra (para log, suporte e mensagens: "por que o cupom não valeu?").</summary>
    public abstract string Descricao { get; }

    public Especificacao<T> E(Especificacao<T> outra) => new EspecificacaoE<T>(this, outra);

    public Especificacao<T> Ou(Especificacao<T> outra) => new EspecificacaoOu<T>(this, outra);

    public Especificacao<T> Nao() => new EspecificacaoNao<T>(this);

    public static Especificacao<T> operator &(Especificacao<T> esquerda, Especificacao<T> direita) => esquerda.E(direita);

    public static Especificacao<T> operator |(Especificacao<T> esquerda, Especificacao<T> direita) => esquerda.Ou(direita);

    public static Especificacao<T> operator !(Especificacao<T> especificacao) => especificacao.Nao();

    public override string ToString() => Descricao;
}

/// <summary>Satisfeita quando AMBAS são satisfeitas (curto-circuito). Descrição: "(A E B)".</summary>
public sealed class EspecificacaoE<T>(Especificacao<T> esquerda, Especificacao<T> direita) : Especificacao<T>
{
    public override bool EhSatisfeitaPor(T candidato) =>
        esquerda.EhSatisfeitaPor(candidato) && direita.EhSatisfeitaPor(candidato);

    public override string Descricao => $"({esquerda.Descricao} E {direita.Descricao})";
}

/// <summary>Satisfeita quando PELO MENOS UMA é satisfeita (curto-circuito). Descrição: "(A OU B)".</summary>
public sealed class EspecificacaoOu<T>(Especificacao<T> esquerda, Especificacao<T> direita) : Especificacao<T>
{
    public override bool EhSatisfeitaPor(T candidato) =>
        esquerda.EhSatisfeitaPor(candidato) || direita.EhSatisfeitaPor(candidato);

    public override string Descricao => $"({esquerda.Descricao} OU {direita.Descricao})";
}

/// <summary>Inverte a regra. Descrição: "NÃO A".</summary>
public sealed class EspecificacaoNao<T>(Especificacao<T> interna) : Especificacao<T>
{
    public override bool EhSatisfeitaPor(T candidato) => !interna.EhSatisfeitaPor(candidato);

    public override string Descricao => $"NÃO {interna.Descricao}";
}
