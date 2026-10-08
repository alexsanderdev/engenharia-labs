using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Dominio;
using F4M04.Cqrs.Pedidos.Infra;
using FluentValidation;

namespace F4M04.Cqrs.Pedidos.Escrita;

/// <summary>Item pedido pelo cliente: só produto e quantidade (preço NUNCA vem de fora).</summary>
public sealed record ItemSolicitado(Guid ProdutoId, int Quantidade);

/// <summary>Command: cria um pedido. Devolve apenas o id gerado.</summary>
public sealed record CriarPedido(Guid ClienteId, IReadOnlyList<ItemSolicitado> Itens) : ICommand<Guid>;

/// <summary>
/// Validação de FORMATO do command (PRONTA, use como modelo): roda no pipeline antes do handler.
/// Regras que dependem de estado (produto inativo, por exemplo) ficam no domínio.
/// </summary>
public sealed class CriarPedidoValidator : AbstractValidator<CriarPedido>
{
    public CriarPedidoValidator()
    {
        RuleFor(c => c.ClienteId).NotEmpty().WithMessage("Informe o cliente.");
        RuleFor(c => c.Itens).NotEmpty().WithMessage("Um pedido precisa de pelo menos um item.");
        RuleForEach(c => c.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId).NotEmpty().WithMessage("Informe o produto.");
            item.RuleFor(i => i.Quantidade).GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
        });
    }
}

/// <summary>
/// Handler do lado de escrita: carrega o que precisa, chama o domínio e registra o agregado
/// no repositório. NÃO chama SaveChanges: quem confirma é o decorator de unidade de trabalho.
/// </summary>
public sealed class CriarPedidoHandler(
    ICatalogo catalogo,
    IRepositorioDePedidos repositorio,
    TimeProvider relogio) : ICommandHandler<CriarPedido, Guid>
{
    public async Task<Guid> HandleAsync(CriarPedido command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var itens = new List<(Produto, int)>();
        foreach (var item in command.Itens)
        {
            var produto = await catalogo.ObterProdutoAsync(item.ProdutoId, ct)
                          ?? throw new RegraDeNegocioException($"Produto {item.ProdutoId} não existe no catálogo.");
            itens.Add((produto, item.Quantidade));
        }

        var pedido = Pedido.Criar(command.ClienteId, itens, relogio.GetUtcNow());
        repositorio.Adicionar(pedido);
        return pedido.Id;
    }
}
