using F5M01.Api.Dominio;

namespace F5M01.Api.Legado;

/// <summary>
/// A API "RPC disfarçada de REST" que o OrderFlow tinha antes deste módulo (PRONTA, e propositalmente RUIM).
/// Leia e anote cada problema antes de começar — ela é o "antes" do lab:
/// <list type="bullet">
/// <item>verbos na URL (<c>/api/CriarPedido</c>, <c>/api/CancelarPedido</c>) e tudo via POST, inclusive leitura;</item>
/// <item>200 OK para tudo, com <c>{ "sucesso": false }</c> no corpo — cache, proxies, retries e monitoramento não enxergam o erro;</item>
/// <item>a entidade de domínio serializada direto: vaza <c>custoInterno</c>, <c>notaInternaAntifraude</c>, <c>versao</c>,
/// e qualquer refatoração do domínio quebra os clientes;</item>
/// <item>erros em texto livre (sem código estável, sem formato padrão);</item>
/// <item>sem paginação, sem ETag, sem concorrência otimista, sem Location.</item>
/// </list>
/// No fim do lab, ela deixa de ser mapeada (Program.cs). O arquivo fica no projeto só como referência do "antes".
/// </summary>
public static class RpcEndpoints
{
    public static IEndpointRouteBuilder MapRpcLegado(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/CriarPedido", (RpcCriarPedido req, ICatalogo catalogo, IPedidoRepositorio repo, TimeProvider relogio) =>
        {
            try
            {
                var pedido = Pedido.Criar(req.ClienteId, relogio.GetUtcNow());
                foreach (var item in req.Itens ?? [])
                {
                    var produto = catalogo.Obter(item.ProdutoId) ?? throw new InvalidOperationException("produto nao existe");
                    var erro = pedido.DefinirItem(produto, item.Quantidade, out _);
                    if (erro is not null) return Results.Ok(new { sucesso = false, erro = erro.Mensagem });
                }
                repo.Adicionar(pedido);
                return Results.Ok(new { sucesso = true, pedido });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { sucesso = false, erro = ex.Message });
            }
        });

        api.MapPost("/ObterPedido", (RpcId req, IPedidoRepositorio repo) =>
        {
            var pedido = repo.Obter(req.Id);
            return pedido is null
                ? Results.Ok(new { sucesso = false, erro = "Pedido nao encontrado" })
                : Results.Ok(new { sucesso = true, pedido });
        });

        api.MapPost("/ListarPedidos", (IPedidoRepositorio repo) => Results.Ok(new { sucesso = true, pedidos = repo.Listar() }));

        api.MapPost("/CancelarPedido", (RpcId req, IPedidoRepositorio repo) =>
        {
            var pedido = repo.Obter(req.Id);
            if (pedido is null) return Results.Ok(new { sucesso = false, erro = "Pedido nao encontrado" });
            var erro = pedido.Cancelar();
            return Results.Ok(new { sucesso = erro is null, erro = erro?.Mensagem });
        });

        return app;
    }

    public sealed record RpcCriarPedido(Guid ClienteId, List<RpcItem>? Itens);
    public sealed record RpcItem(Guid ProdutoId, int Quantidade);
    public sealed record RpcId(Guid Id);
}
