using F2M08.Domain.Descontos;
using Microsoft.EntityFrameworkCore;

namespace F2M08.Domain.Entidades;

/// <summary>
/// Agregado Pedido. Regras: quantidade &gt; 0, produto inativo não entra, itens só mudam em Created,
/// pedido vazio não confirma, Created → Confirmed → Completed, Created → Cancelled, Completed não cancela.
/// Subtotal, desconto e total são SEMPRE calculados aqui.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    public Pedido(Guid id, Guid clienteId)
    {
        Id = id;
        ClienteId = clienteId;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; private set; }

    // TODO (Passo 3): com setter público, "pedido.Status = StatusPedido.Completed" pula todas as transições.
    public Guid ClienteId { get; set; }
    public StatusPedido Status { get; set; }

    public IReadOnlyList<ItemPedido> Itens => _itens;

    public int Unidades => _itens.Sum(i => i.Quantidade);
    public decimal Subtotal => _itens.Sum(i => i.Subtotal);
    public decimal Desconto => PoliticaDeDesconto.CalcularDesconto(Subtotal, Unidades);
    public decimal Total => Subtotal - Desconto;

    public void AdicionarItem(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException("Só é possível alterar itens de um pedido Created.");
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), quantidade, "A quantidade deve ser maior que zero.");
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");

        var existente = _itens.Find(i => i.ProdutoId == produto.Id);
        if (existente is not null)
            existente.Somar(quantidade);
        else
            _itens.Add(new ItemPedido(produto.Id, quantidade, produto.Preco));
    }

    public void Confirmar()
    {
        if (_itens.Count == 0)
            throw new InvalidOperationException("Pedido sem itens não pode ser confirmado.");
        Transicionar(StatusPedido.Created, StatusPedido.Confirmed);
    }

    public void Concluir() => Transicionar(StatusPedido.Confirmed, StatusPedido.Completed);

    public void Cancelar()
    {
        if (Status == StatusPedido.Completed)
            throw new InvalidOperationException("Pedido concluído não pode ser cancelado.");
        Transicionar(StatusPedido.Created, StatusPedido.Cancelled);
    }

    private void Transicionar(StatusPedido de, StatusPedido para)
    {
        if (Status != de)
            throw new InvalidOperationException($"Transição inválida: {Status} → {para}.");
        Status = para;
    }

    /// <summary>
    /// "Atalho" que alguém achou prático: a entidade se mapeia sozinha.
    /// TODO (Passo 1): isto arrasta EF Core para dentro do domínio. Mova este código para
    /// <c>F2M08.Infrastructure/Persistencia/PedidoConfiguration.cs</c> (uma
    /// <c>IEntityTypeConfiguration&lt;Pedido&gt;</c>), aplique-a no <c>OrderFlowDbContext</c>,
    /// apague este método e a referência a EF Core do <c>F2M08.Domain.csproj</c>.
    /// </summary>
    public static void ConfigurarMapeamento(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var builder = modelBuilder.Entity<Pedido>();
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => p.ClienteId);
        builder.Ignore(p => p.Unidades);
        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.Desconto);
        builder.Ignore(p => p.Total);

        builder.OwnsMany(p => p.Itens, item =>
        {
            item.ToTable("ItensPedido");
            item.WithOwner().HasForeignKey("PedidoId");
            item.Property<int>("ItemId");
            item.HasKey("ItemId");
            item.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            item.Ignore(i => i.Subtotal);
        });
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
