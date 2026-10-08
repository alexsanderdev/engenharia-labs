namespace F3M04.EfCore.Dominio;

/// <summary>Cliente do OrderFlow. O e-mail é único (regra garantida pelo banco).</summary>
public sealed class Cliente
{
    /// <summary>Construtor usado pelo EF Core na materialização.</summary>
    private Cliente() { }

    public Cliente(Guid id, string nome, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Id = id;
        Nome = nome;
        Email = email;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Email { get; private set; } = "";
}
