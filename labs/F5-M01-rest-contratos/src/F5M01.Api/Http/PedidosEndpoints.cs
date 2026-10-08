using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using F5M01.Api.Contratos;
using F5M01.Api.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace F5M01.Api.Http;

/// <summary>
/// A API REST de Pedidos. Recursos e sub-recursos, métodos com a semântica certa, status codes certos,
/// contrato separado do domínio, ETag/If-None-Match/If-Match, paginação e JSON Merge Patch.
/// </summary>
public static partial class PedidosEndpoints
{
    public static IEndpointRouteBuilder MapPedidosEndpoints(this IEndpointRouteBuilder app)
    {
        var pedidos = app.MapGroup("/pedidos").WithTags("Pedidos");

        pedidos.MapGet("/", Listar);
        pedidos.MapPost("/", Criar);
        pedidos.MapGet("/{id:guid}", Obter);
        pedidos.MapPatch("/{id:guid}", AtualizarParcial);

        // Sub-recurso: os itens pertencem ao pedido. O produto identifica o item dentro do pedido.
        pedidos.MapGet("/{id:guid}/itens", ListarItens);
        pedidos.MapPut("/{id:guid}/itens/{produtoId:guid}", DefinirItem);
        pedidos.MapDelete("/{id:guid}/itens/{produtoId:guid}", RemoverItem);

        // Transições de estado modeladas como criação de um sub-recurso ("a confirmação do pedido").
        pedidos.MapPost("/{id:guid}/confirmacao", (Guid id, HttpContext http, IPedidoRepositorio repo) =>
            Transicionar(id, http, repo, p => p.Confirmar()));
        pedidos.MapPost("/{id:guid}/cancelamento", (Guid id, HttpContext http, IPedidoRepositorio repo) =>
            Transicionar(id, http, repo, p => p.Cancelar()));

        return app;
    }

    // ---------------------------------------------------------------- coleção

    private static IResult Listar([AsParameters] ConsultaPedidosQuery query, HttpContext http, IPedidoRepositorio repo)
    {
        var erros = ConsultaPedidos.Validar(query);
        if (erros.Count > 0) return Problemas.Validacao(erros);
        return TypedResults.Ok(ConsultaPedidos.Executar(repo.Listar(), query, http.Request.Path));
    }

    private static IResult Criar(CriarPedidoRequest request, HttpContext http, ICatalogo catalogo, IPedidoRepositorio repo, TimeProvider relogio)
    {
        // 1) Formato (400): junta TODOS os erros de campo antes de responder.
        var erros = new Dictionary<string, string[]>();
        if (request.ClienteId == Guid.Empty) erros["clienteId"] = ["Informe o cliente."];
        if (request.Itens is null || request.Itens.Count == 0) erros["itens"] = ["O pedido precisa de pelo menos um item."];
        for (var i = 0; i < (request.Itens?.Count ?? 0); i++)
        {
            if (request.Itens![i].Quantidade <= 0) erros[$"itens[{i}].quantidade"] = ["A quantidade deve ser maior que zero."];
        }
        if (request.Observacao is { Length: > TamanhoMaximoObservacao })
            erros["observacao"] = [$"A observação aceita no máximo {TamanhoMaximoObservacao} caracteres."];
        if (erros.Count > 0) return Problemas.Validacao(erros);

        // 2) Regras que dependem de estado (422).
        var pedido = Pedido.Criar(request.ClienteId, relogio.GetUtcNow());
        foreach (var item in request.Itens!)
        {
            var produto = catalogo.Obter(item.ProdutoId);
            if (produto is null) return Problemas.De(ErrosPedido.ProdutoInexistente);
            var erro = pedido.DefinirItem(produto, item.Quantidade, out _);
            if (erro is not null) return Problemas.De(erro);
        }
        if (request.Observacao is not null) pedido.AtualizarDadosEntrega(request.Observacao, null);

        repo.Adicionar(pedido);

        // 3) 201 + Location (onde o recurso novo mora) + ETag (já dá para fazer GET condicional / If-Match).
        http.Response.Headers.ETag = ETags.Para(pedido);
        return TypedResults.Created($"/pedidos/{pedido.Id}", PedidoResponse.De(pedido));
    }

    // ---------------------------------------------------------------- item da coleção

    private static IResult Obter(Guid id, HttpContext http, IPedidoRepositorio repo)
    {
        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);

        var etag = ETags.Para(pedido);
        http.Response.Headers.ETag = etag;
        // no-cache = "pode guardar, mas revalide sempre" (com o ETag, a revalidação custa um 304 sem corpo).
        // private = só o cache do cliente; proxies compartilhados não guardam dados de um cliente.
        http.Response.Headers.CacheControl = "private, no-cache";

        if (ETags.IfNoneMatchCorresponde(http.Request.Headers.IfNoneMatch, etag))
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);

        return TypedResults.Ok(PedidoResponse.De(pedido));
    }

    private static async Task<IResult> AtualizarParcial(Guid id, HttpContext http, IPedidoRepositorio repo)
    {
        // 415 antes de tudo: só aceitamos JSON Merge Patch (application/json teria semântica ambígua).
        if (!MediaTypeHeaderValue.TryParse(http.Request.ContentType, out var contentType)
            || !contentType.MediaType.Equals(MergePatch.MediaType, StringComparison.OrdinalIgnoreCase))
            return Problemas.MediaTypeNaoSuportado(MergePatch.MediaType);

        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);
        if (!ETags.IfMatchAtendido(http.Request.Headers.IfMatch, ETags.Para(pedido))) return Problemas.VersaoDesatualizada();

        JsonNode? patch;
        try
        {
            patch = await JsonNode.ParseAsync(http.Request.Body, cancellationToken: http.RequestAborted);
        }
        catch (JsonException)
        {
            return Problemas.JsonInvalido();
        }
        if (patch is not JsonObject patchObjeto) return Problemas.JsonInvalido();

        // Só estes campos são editáveis por PATCH. Status, itens e total têm operações próprias.
        var erros = new Dictionary<string, string[]>();
        foreach (var (nome, _) in patchObjeto)
        {
            if (nome is not (CampoObservacao or CampoEndereco)) erros[nome] = ["Campo não editável por PATCH."];
        }
        if (erros.Count > 0) return Problemas.Validacao(erros, StatusCodes.Status422UnprocessableEntity);

        // Documento editável atual → aplica o merge patch → valida o RESULTADO (não o patch).
        var atual = JsonSerializer.SerializeToNode(new DadosEntrega(
            pedido.Observacao,
            pedido.EnderecoEntrega is { } e ? new EnderecoContrato(e.Logradouro, e.Cidade, e.Cep) : null), JsonWeb);
        var mesclado = MergePatch.Aplicar(atual, patchObjeto);

        DadosEntrega? novo;
        try
        {
            novo = mesclado.Deserialize<DadosEntrega>(JsonWeb);
        }
        catch (JsonException)
        {
            return Problemas.Validacao(new Dictionary<string, string[]> { ["corpo"] = ["Tipos inválidos no patch."] },
                StatusCodes.Status422UnprocessableEntity);
        }

        erros = ValidarDadosEntrega(novo!);
        if (erros.Count > 0) return Problemas.Validacao(erros, StatusCodes.Status422UnprocessableEntity);

        var erro = pedido.AtualizarDadosEntrega(
            novo!.Observacao,
            novo.EnderecoEntrega is { } n ? new Endereco(n.Logradouro, n.Cidade, n.Cep) : null);
        if (erro is not null) return Problemas.De(erro);

        http.Response.Headers.ETag = ETags.Para(pedido);
        return TypedResults.Ok(PedidoResponse.De(pedido));
    }

    private static IResult Transicionar(Guid id, HttpContext http, IPedidoRepositorio repo, Func<Pedido, ErroDominio?> transicao)
    {
        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);
        if (!ETags.IfMatchAtendido(http.Request.Headers.IfMatch, ETags.Para(pedido))) return Problemas.VersaoDesatualizada();

        var erro = transicao(pedido);
        if (erro is not null) return Problemas.De(erro);

        http.Response.Headers.ETag = ETags.Para(pedido);
        return TypedResults.Ok(PedidoResponse.De(pedido));
    }

    // ---------------------------------------------------------------- sub-recurso itens

    private static IResult ListarItens(Guid id, IPedidoRepositorio repo)
    {
        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);
        return TypedResults.Ok(pedido.Itens.Select(ItemPedidoResponse.De).ToList());
    }

    private static IResult DefinirItem(Guid id, Guid produtoId, DefinirItemRequest request, HttpContext http, ICatalogo catalogo, IPedidoRepositorio repo)
    {
        if (request.Quantidade <= 0)
            return Problemas.Validacao(new Dictionary<string, string[]> { ["quantidade"] = ["A quantidade deve ser maior que zero."] });

        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);
        if (!ETags.IfMatchAtendido(http.Request.Headers.IfMatch, ETags.Para(pedido))) return Problemas.VersaoDesatualizada();

        var produto = catalogo.Obter(produtoId);
        if (produto is null) return Problemas.De(ErrosPedido.ProdutoInexistente);

        var erro = pedido.DefinirItem(produto, request.Quantidade, out var criado);
        if (erro is not null) return Problemas.De(erro);

        http.Response.Headers.ETag = ETags.Para(pedido);
        var item = ItemPedidoResponse.De(pedido.Itens.Single(i => i.ProdutoId == produtoId));
        // PUT que cria → 201 + Location; PUT que substitui (ou repete) → 200. O EFEITO é o mesmo: idempotente.
        return criado
            ? TypedResults.Created($"/pedidos/{id}/itens/{produtoId}", item)
            : TypedResults.Ok(item);
    }

    private static IResult RemoverItem(Guid id, Guid produtoId, HttpContext http, IPedidoRepositorio repo)
    {
        var pedido = repo.Obter(id);
        if (pedido is null) return Problemas.PedidoNaoEncontrado(id);
        if (!ETags.IfMatchAtendido(http.Request.Headers.IfMatch, ETags.Para(pedido))) return Problemas.VersaoDesatualizada();

        var erro = pedido.RemoverItem(produtoId);
        if (erro is not null) return Problemas.De(erro);

        http.Response.Headers.ETag = ETags.Para(pedido);
        // Remover o que já não existe dá o mesmo estado final: 204 nas duas chamadas (DELETE idempotente).
        return TypedResults.NoContent();
    }

    // ---------------------------------------------------------------- apoio (PRONTO)

    public const int TamanhoMaximoObservacao = 500;
    private const string CampoObservacao = "observacao";
    private const string CampoEndereco = "enderecoEntrega";
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    /// <summary>A parte do pedido editável por PATCH (o "documento" sobre o qual o merge patch é aplicado).</summary>
    private sealed record DadosEntrega(string? Observacao, EnderecoContrato? EnderecoEntrega);

    private static Dictionary<string, string[]> ValidarDadosEntrega(DadosEntrega dados)
    {
        var erros = new Dictionary<string, string[]>();
        if (dados.Observacao is { Length: > TamanhoMaximoObservacao })
            erros[CampoObservacao] = [$"A observação aceita no máximo {TamanhoMaximoObservacao} caracteres."];
        if (dados.EnderecoEntrega is { } e)
        {
            if (string.IsNullOrWhiteSpace(e.Logradouro)) erros["enderecoEntrega.logradouro"] = ["Informe o logradouro."];
            if (string.IsNullOrWhiteSpace(e.Cidade)) erros["enderecoEntrega.cidade"] = ["Informe a cidade."];
            if (e.Cep is null || !CepRegex().IsMatch(e.Cep)) erros["enderecoEntrega.cep"] = ["CEP inválido (formato 00000-000)."];
        }
        return erros;
    }

    [GeneratedRegex(@"^\d{5}-?\d{3}$")]
    private static partial Regex CepRegex();
}
