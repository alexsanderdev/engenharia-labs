namespace F1M07.Api.Products;

/// <summary>Produto do catálogo do OrderFlow (modelo interno, nunca exposto direto na API).</summary>
public sealed record Product(Guid Id, string Name, decimal Price, bool IsActive);

/// <summary>Corpo de entrada de POST/PUT. Tudo anulável: quem valida é o endpoint filter.</summary>
public sealed record ProductRequest(string? Name, decimal Price);

/// <summary>Contrato de saída da API (DTO).</summary>
public sealed record ProductResponse(Guid Id, string Name, decimal Price, bool IsActive)
{
    public static ProductResponse From(Product p) => new(p.Id, p.Name, p.Price, p.IsActive);
}
