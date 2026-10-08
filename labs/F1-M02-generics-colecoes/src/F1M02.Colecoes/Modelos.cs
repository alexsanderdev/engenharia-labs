namespace F1M02.Colecoes;

/// <summary>Qualquer entidade com identidade do tipo <typeparamref name="TId"/>.</summary>
public interface IEntidade<TId> where TId : notnull
{
    TId Id { get; }
}

/// <summary>Qualquer coisa que tenha preço.</summary>
public interface IComPreco
{
    decimal Preco { get; }
}

/// <summary>Produto do catálogo (id Guid).</summary>
public sealed record Produto(Guid Id, string Nome, decimal Preco, string Categoria) : IEntidade<Guid>, IComPreco;

/// <summary>Cliente do OrderFlow (id = e-mail).</summary>
public sealed record Cliente(string Id, string Nome) : IEntidade<string>;

/// <summary>Prioridade de preparo na cozinha. Menor valor = sai primeiro.</summary>
public enum PrioridadeDePreparo
{
    Expressa = 0,
    Normal = 1,
    Agendada = 2
}
