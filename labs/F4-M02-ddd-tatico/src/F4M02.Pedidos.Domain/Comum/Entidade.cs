namespace F4M02.Pedidos.Domain.Comum;

/// <summary>
/// Base de toda entidade: um objeto definido pela sua <b>identidade</b>, não pelos seus atributos.
/// Dois objetos com o mesmo <see cref="Id"/> (e do mesmo tipo) são a mesma entidade, mesmo que o estado
/// carregado em memória seja diferente; dois objetos com atributos iguais e ids diferentes são entidades diferentes.
/// </summary>
/// <remarks>PRONTO — leia, não precisa alterar.</remarks>
/// <typeparam name="TId">Tipo da identidade (prefira ids fortemente tipados, como <see cref="PedidoId"/>).</typeparam>
public abstract class Entidade<TId> : IEquatable<Entidade<TId>>
    where TId : notnull
{
    protected Entidade(TId id) => Id = id;

    /// <summary>Identidade da entidade. Não muda durante todo o ciclo de vida.</summary>
    public TId Id { get; }

    public bool Equals(Entidade<TId>? other) =>
        other is not null
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => Equals(obj as Entidade<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entidade<TId>? esquerda, Entidade<TId>? direita) =>
        esquerda is null ? direita is null : esquerda.Equals(direita);

    public static bool operator !=(Entidade<TId>? esquerda, Entidade<TId>? direita) => !(esquerda == direita);
}
