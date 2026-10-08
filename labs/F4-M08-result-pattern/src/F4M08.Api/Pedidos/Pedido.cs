using F4M08.Api.Resultados;

namespace F4M08.Api.Pedidos;

public enum StatusPedido { Created, Confirmed, Completed, Cancelled }

public sealed record Produto(Guid Id, string Nome, decimal Preco, bool Ativo);

public sealed record ItemPedido(Guid ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario)
{
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

/// <summary>
/// Catálogo de erros do módulo Pedidos. Os CÓDIGOS são contrato público (clientes fazem
/// <c>if (problem.code == "pedido.transicao_invalida")</c>): nunca mude um código existente.
/// </summary>
public static class PedidoErrors
{
    public const string CodigoValidacao = "pedido.validacao";
    public const string CodigoNaoEncontrado = "pedido.nao_encontrado";
    public const string CodigoProdutoInexistente = "pedido.produto_inexistente";
    public const string CodigoProdutoInativo = "pedido.produto_inativo";
    public const string CodigoTransicaoInvalida = "pedido.transicao_invalida";
    public const string CodigoAcessoNegado = "pedido.acesso_negado";

    public static Error Validacao(IReadOnlyDictionary<string, string[]> erros) =>
        Error.Validation(CodigoValidacao, "Um ou mais campos do pedido são inválidos.", erros);

    public static Error NaoEncontrado(Guid pedidoId) =>
        Error.NotFound(CodigoNaoEncontrado, $"Pedido {pedidoId} não encontrado.");

    public static Error ProdutoInexistente(Guid produtoId) =>
        Error.Failure(CodigoProdutoInexistente, $"O produto {produtoId} não existe no catálogo.");

    public static Error ProdutoInativo(string nomeProduto) =>
        Error.Failure(CodigoProdutoInativo, $"O produto '{nomeProduto}' está inativo e não pode entrar em pedido.");

    public static Error TransicaoInvalida(StatusPedido atual, StatusPedido destino) =>
        Error.Conflict(CodigoTransicaoInvalida, $"Não é possível passar o pedido de {atual} para {destino}.");

    public static Error AcessoNegado(Guid pedidoId) =>
        Error.Forbidden(CodigoAcessoNegado, $"Você não tem permissão para alterar o pedido {pedidoId}.");
}

/// <summary>
/// Agregado Pedido (PRONTO). Repare: nenhuma regra de negócio lança exceção.
/// Violações viram <see cref="Error"/> e voltam dentro de um <see cref="Result"/>.
/// Exceções aqui só para BUG de programação (argumento nulo).
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public DateTimeOffset CriadoEm { get; }
    public StatusPedido Status { get; private set; } = StatusPedido.Created;
    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Total => _itens.Sum(i => i.Subtotal);

    /// <summary>Cria o pedido com preços do catálogo. Produto inativo → <see cref="PedidoErrors.ProdutoInativo"/>.</summary>
    public static Result<Pedido> Criar(Guid clienteId, IReadOnlyList<(Produto Produto, int Quantidade)> itens, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(itens);
        var pedido = new Pedido(Guid.CreateVersion7(agora), clienteId, agora);
        foreach (var (produto, quantidade) in itens)
        {
            if (!produto.Ativo) return PedidoErrors.ProdutoInativo(produto.Nome);
            pedido._itens.Add(new ItemPedido(produto.Id, produto.Nome, quantidade, produto.Preco));
        }
        return pedido;
    }

    /// <summary>Created → Confirmed.</summary>
    public Result Confirmar() => MudarStatus(StatusPedido.Created, StatusPedido.Confirmed);

    /// <summary>Confirmed → Completed.</summary>
    public Result Concluir() => MudarStatus(StatusPedido.Confirmed, StatusPedido.Completed);

    /// <summary>Created/Confirmed → Cancelled. Completed (ou já cancelado) não cancela.</summary>
    public Result Cancelar()
    {
        if (Status is StatusPedido.Completed or StatusPedido.Cancelled)
            return PedidoErrors.TransicaoInvalida(Status, StatusPedido.Cancelled);
        Status = StatusPedido.Cancelled;
        return Result.Success();
    }

    private Result MudarStatus(StatusPedido origemEsperada, StatusPedido destino)
    {
        if (Status != origemEsperada) return PedidoErrors.TransicaoInvalida(Status, destino);
        Status = destino;
        return Result.Success();
    }
}
