using F4M01.Application.Abstracoes;
using F4M01.Domain.Pedidos;

namespace F4M01.Application.Pedidos;

/// <summary>Entrada do caso de uso. Repare: NÃO existe preço aqui — preço vem do catálogo.</summary>
public sealed record CriarPedidoCommand(Guid ClienteId, IReadOnlyList<ItemDoPedidoCommand> Itens);

public sealed record ItemDoPedidoCommand(Guid ProdutoId, int Quantidade);

/// <summary>Saída do caso de uso: um DTO, nunca a entidade (quem chama não pode alterar o agregado).</summary>
public sealed record PedidoCriadoDto(Guid Id, decimal Total, DateTimeOffset CriadoEm);

/// <summary>
/// Caso de uso "Criar pedido": orquestra (carrega produtos → domínio cria → persiste → devolve DTO).
/// A REGRA mora no <see cref="Pedido"/>; aqui fica só o fluxo. Depende apenas de portas.
/// </summary>
public sealed class CriarPedidoHandler(IPedidoRepository pedidos, IProdutoRepository produtos, IRelogio relogio)
{
    /// <exception cref="ProdutoNaoEncontradoException">Algum produto do comando não existe.</exception>
    /// <exception cref="F4M01.Domain.Comum.DomainException">Regra de negócio violada.</exception>
    public Task<PedidoCriadoDto> HandleAsync(CriarPedidoCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        _ = (pedidos, produtos, relogio, ct);

        throw new NotImplementedException(
            "TODO (Passo 3): orquestre o caso de uso SÓ com as portas: " +
            "1) ids distintos dos itens → UMA chamada a produtos.ObterPorIdsAsync; " +
            "2) id que não voltou → ProdutoNaoEncontradoException(id); " +
            "3) Pedido.Criar(command.ClienteId, relogio.Agora, linhas) — a regra é do domínio; " +
            "4) pedidos.AdicionarAsync; 5) devolva PedidoCriadoDto (nunca a entidade).");
    }
}
