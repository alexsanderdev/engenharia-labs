using F4M01.Domain.Comum;
using F4M01.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace F4M01.Domain.Pedidos;

/// <summary>
/// Pedido — hoje um saco de propriedades (modelo anêmico). Toda a regra está no endpoint POST /pedidos da Api.
/// </summary>
/// <remarks>
/// TODO (Passo 2):
/// 1. Remova o <c>[Precision]</c> (e o using de EF Core): o mapeamento vai para a Infrastructure (Fluent API).
/// 2. Setters <c>private</c>; construtor sem parâmetros <c>private</c> (o EF Core usa).
/// 3. Itens: campo <c>List&lt;ItemPedido&gt; _itens</c> exposto como <c>IReadOnlyList&lt;ItemPedido&gt; Itens</c>.
/// </remarks>
public sealed class Pedido
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public StatusPedido Status { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    public List<ItemPedido> Itens { get; set; } = [];

    /// <summary>
    /// Fábrica do agregado: valida TODAS as regras antes de devolver um pedido.
    /// Se retornou, o pedido é válido.
    /// </summary>
    /// <exception cref="DomainException">Sem itens, quantidade &lt;= 0 ou produto inativo.</exception>
    public static Pedido Criar(Guid clienteId, DateTimeOffset criadoEm, IReadOnlyList<(Produto Produto, int Quantidade)> linhas) =>
        throw new NotImplementedException(
            "TODO (Passo 2): mova para cá as regras que estão no endpoint POST /pedidos (Program.cs da Api): " +
            "sem itens → DomainException(\"O pedido precisa ter pelo menos um item.\"); quantidade <= 0 → " +
            "DomainException(\"A quantidade de {nome} deve ser maior que zero.\"); produto inativo → " +
            "DomainException(\"O produto {nome} está inativo e não pode entrar em pedido.\"). " +
            "Novo Guid, status Created, itens com o preço do PRODUTO e Total = soma dos subtotais.");
}
