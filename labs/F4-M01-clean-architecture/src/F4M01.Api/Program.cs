using F4M01.Application.Pedidos;
using F4M01.Domain.Pedidos;
using F4M01.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

// ============================================================================================
// CÓDIGO INICIAL "GORDO": tudo no Program.cs. Funciona, mas:
//  - regra de negócio (itens, quantidade, produto ativo, total) mora no endpoint;
//  - o endpoint fala direto com EF Core (DbContext, ToListAsync, SaveChangesAsync);
//  - o preço vem do CLIENTE (request) e não do catálogo;
//  - DateTime.UtcNow escondido (teste não controla o tempo);
//  - a resposta devolve a ENTIDADE Pedido (contrato HTTP acoplado ao modelo de domínio/banco).
// TODO (Passos 4 e 5): endpoint magro em Endpoints/PedidoEndpoints.cs chamando CriarPedidoHandler;
// composition root em Composicao/ComposicaoDaApi.cs (único lugar que chama AddInfrastructure);
// erros de domínio → ProblemDetails num IExceptionHandler.
// ============================================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddDbContext<OrderFlowDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("OrderFlow")));
builder.Services.AddScoped<ListarPedidosDoClienteHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapPost("/pedidos", async (CriarPedidoLegadoRequest request, OrderFlowDbContext db, CancellationToken ct) =>
{
    if (request.Itens is null || request.Itens.Count == 0)
        return Results.BadRequest("O pedido precisa ter pelo menos um item.");

    var ids = request.Itens.Select(i => i.ProdutoId).ToList();
    var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    var pedido = new Pedido
    {
        Id = Guid.NewGuid(),
        ClienteId = request.ClienteId,
        CriadoEm = DateTime.UtcNow,
        Status = StatusPedido.Created,
    };

    foreach (var item in request.Itens)
    {
        var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId);
        if (produto is null)
            return Results.NotFound($"Produto não encontrado: {item.ProdutoId}");
        if (item.Quantidade <= 0)
            return Results.BadRequest($"A quantidade de {produto.Nome} deve ser maior que zero.");
        if (!produto.Ativo)
            return Results.BadRequest($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");

        // Preço do request: o cliente decide quanto paga (!)
        pedido.Itens.Add(new ItemPedido(produto.Id, produto.Nome, item.Quantidade, item.PrecoUnitario));
        pedido.Total += item.PrecoUnitario * item.Quantidade;
    }

    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/pedidos/{pedido.Id}", pedido);
});

app.MapGet("/clientes/{clienteId:guid}/pedidos", async (Guid clienteId, ListarPedidosDoClienteHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(clienteId, ct)));

app.Run();

/// <summary>Contrato antigo: aceita preço vindo do cliente.</summary>
internal sealed record CriarPedidoLegadoRequest(Guid ClienteId, List<CriarPedidoLegadoItem> Itens);

internal sealed record CriarPedidoLegadoItem(Guid ProdutoId, int Quantidade, decimal PrecoUnitario);

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
