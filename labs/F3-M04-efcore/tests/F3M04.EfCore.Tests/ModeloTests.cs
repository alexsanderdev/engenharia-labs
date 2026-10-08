using F3M04.EfCore.Dominio;
using F3M04.EfCore.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace F3M04.EfCore.Tests;

/// <summary>
/// Passos 1 a 4: o MODELO do EF Core (metadata), sem tocar no banco. É o que o EF "entendeu"
/// das suas configurações — e é dele que saem o schema e as migrations.
/// </summary>
public sealed class ModeloTests
{
    private static IModel Modelo()
    {
        // Não abre conexão: só constrói o modelo. O design-time model guarda tudo (inclusive o seed).
        var options = new DbContextOptionsBuilder<OrderFlowDbContext>()
            .UseSqlServer("Server=nao-conecta;Database=Modelo;Trusted_Connection=True")
            .Options;
        using var db = new OrderFlowDbContext(options);
        return db.GetService<IDesignTimeModel>().Model;
    }

    private static IEntityType Entidade<T>() =>
        Modelo().FindEntityType(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} não está no modelo.");

    [Fact]
    public void Modelo_TabelasEChaves_NomesExplicitosEGuidGeradoPeloDominio()
    {
        Entidade<Cliente>().GetTableName().ShouldBe("Clientes");
        Entidade<Produto>().GetTableName().ShouldBe("Produtos");
        Entidade<Pedido>().GetTableName().ShouldBe("Pedidos");
        Entidade<ItemPedido>().GetTableName().ShouldBe("ItensPedido");

        foreach (var tipo in new[] { typeof(Cliente), typeof(Produto), typeof(Pedido), typeof(ItemPedido) })
        {
            var chave = Modelo().FindEntityType(tipo)!.FindPrimaryKey()!;
            chave.Properties.Single().Name.ShouldBe("Id", $"chave de {tipo.Name}");
            chave.Properties.Single().ValueGenerated.ShouldBe(ValueGenerated.Never, $"o Id de {tipo.Name} é gerado no construtor");
        }
    }

    [Fact]
    public void ProdutoECliente_ObrigatoriosTamanhosEIndicesUnicos()
    {
        var produto = Entidade<Produto>();
        var sku = produto.FindProperty(nameof(Produto.Sku))!;
        sku.IsNullable.ShouldBeFalse();
        sku.GetMaxLength().ShouldBe(30);
        var nome = produto.FindProperty(nameof(Produto.Nome))!;
        nome.IsNullable.ShouldBeFalse();
        nome.GetMaxLength().ShouldBe(100);
        produto.GetIndexes()
            .ShouldContain(i => i.IsUnique && i.Properties.Single().Name == nameof(Produto.Sku), "SKU é único");

        var cliente = Entidade<Cliente>();
        cliente.FindProperty(nameof(Cliente.Nome))!.GetMaxLength().ShouldBe(100);
        cliente.FindProperty(nameof(Cliente.Email))!.GetMaxLength().ShouldBe(200);
        cliente.GetIndexes()
            .ShouldContain(i => i.IsUnique && i.Properties.Single().Name == nameof(Cliente.Email), "e-mail é único");
    }

    [Fact]
    public void ValoresMonetarios_TemPrecisao18Escala2()
    {
        var propriedades = new[]
        {
            Entidade<Produto>().FindProperty(nameof(Produto.Preco))!,
            Entidade<Pedido>().FindProperty(nameof(Pedido.Total))!,
            Entidade<ItemPedido>().FindProperty(nameof(ItemPedido.PrecoUnitario))!,
        };

        foreach (var propriedade in propriedades)
        {
            propriedade.GetPrecision().ShouldBe(18, propriedade.Name);
            propriedade.GetScale().ShouldBe(2, propriedade.Name);
        }
    }

    [Fact]
    public void Pedido_Status_ConvertidoParaTextoComTamanho20()
    {
        var status = Entidade<Pedido>().FindProperty(nameof(Pedido.Status))!;

        // HasConversion<string>() define o tipo do provider; sem isso, o enum vira int no banco.
        status.GetProviderClrType().ShouldBe(typeof(string), "sem conversão, o enum vira int no banco");
        status.GetMaxLength().ShouldBe(20);
    }

    [Fact]
    public void Pedido_Itens_UmParaMuitosObrigatorioComCascade()
    {
        var fk = Entidade<ItemPedido>().GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Pedido));

        fk.ShouldNotBeNull();
        fk.IsRequired.ShouldBeTrue();
        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        fk.PrincipalToDependent!.Name.ShouldBe(nameof(Pedido.Itens));
        Entidade<ItemPedido>().FindProperty(nameof(ItemPedido.Subtotal)).ShouldBeNull("Subtotal é calculado, não é coluna");
    }

    [Fact]
    public void ReferenciasPorId_TemFkRestrictParaClienteEProduto()
    {
        var pedidoParaCliente = Entidade<Pedido>().GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Cliente));
        pedidoParaCliente.ShouldNotBeNull("Pedido.ClienteId precisa de FK de verdade para Clientes");
        pedidoParaCliente.Properties.Single().Name.ShouldBe(nameof(Pedido.ClienteId));
        pedidoParaCliente.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);

        var itemParaProduto = Entidade<ItemPedido>().GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Produto));
        itemParaProduto.ShouldNotBeNull("ItemPedido.ProdutoId precisa de FK de verdade para Produtos");
        itemParaProduto.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);

        Entidade<Pedido>().GetIndexes()
            .ShouldContain(i => i.Properties.Single().Name == nameof(Pedido.ClienteId), "consultas por cliente precisam de índice");
    }

    [Fact]
    public void Seed_CatalogoInicial_ConfiguradoComHasData()
    {
        var seed = Entidade<Produto>().GetSeedData().ToList();

        seed.Count.ShouldBe(3);
        seed.Select(s => (Guid)s[nameof(Produto.Id)]!)
            .ShouldBe([CatalogoInicial.CanetaId, CatalogoInicial.CadernoId, CatalogoInicial.MochilaId], ignoreOrder: true);
    }
}
