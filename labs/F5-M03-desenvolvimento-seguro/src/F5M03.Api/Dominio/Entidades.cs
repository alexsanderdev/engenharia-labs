namespace F5M03.Api.Dominio;

// ARQUIVO PRONTO — não precisa alterar.
// Entidades de persistência do OrderFlow. Elas têm campos que o cliente da API
// NUNCA deveria enviar (Status, Total, IsAdmin...) nem receber (CustoInterno, SenhaHash...).
// Por isso não devem aparecer diretamente nos contratos HTTP.

public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class Produto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }

    /// <summary>Custo de aquisição. Dado comercial sensível: nunca sai pela API pública.</summary>
    public decimal CustoInterno { get; set; }

    public bool Ativo { get; set; } = true;
    public byte[]? Imagem { get; set; }
    public string? TipoDaImagem { get; set; }
}

public sealed class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>CPF só com dígitos. Dado pessoal (LGPD): mascarar em respostas e logs.</summary>
    public string Cpf { get; set; } = "";

    public string SenhaHash { get; set; } = "";

    /// <summary>Privilégio administrativo. Só pode ser concedido por um fluxo interno, nunca pelo cadastro público.</summary>
    public bool IsAdmin { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}

public sealed class ItemPedido
{
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public sealed class Pedido
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
    public decimal Total { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Created;

    /// <summary>Soma do custo interno dos itens (margem). Uso interno do backoffice.</summary>
    public decimal CustoTotal { get; set; }

    /// <summary>Anotação do backoffice. Uso interno.</summary>
    public string? ObservacaoInterna { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
