using F4M05.Api.DTOs;
using F4M05.Api.Models;
using F4M05.Api.Repositories;

namespace F4M05.Api.Services;

// CÓDIGO INICIAL: o "PedidoService" faz-tudo. Cada método é um caso de uso diferente,
// mas todos moram juntos, compartilham dependências e crescem sem parar.
// TODO (Passo 2): as regras de criação e de status vão para o domínio (Pedido.Criar, Confirmar, Cancelar).
// TODO (Passo 4): cada método vira um Handler na sua fatia; no fim, este arquivo some.

public interface IPedidoService
{
    PedidoDto Criar(CriarPedidoDto dto);
    PedidoDto Obter(Guid id);
    List<PedidoResumoDto> Listar(Guid? clienteId);
    PedidoDto Confirmar(Guid id);
    PedidoDto Cancelar(Guid id);
}

public sealed class PedidoService(IPedidoRepository pedidos, IProdutoRepository produtos) : IPedidoService
{
    public PedidoDto Criar(CriarPedidoDto dto)
    {
        // Validação DUPLICADA: o controller já valida isto antes de chamar o service.
        if (dto.ClienteId == Guid.Empty)
            throw new ArgumentException("Cliente obrigatório.");
        if (dto.Itens is null || dto.Itens.Count == 0)
            throw new ArgumentException("O pedido precisa ter pelo menos um item.");

        var pedido = new Pedido { Id = Guid.NewGuid(), ClienteId = dto.ClienteId, Status = StatusPedido.Created };

        foreach (var item in dto.Itens)
        {
            if (item.Quantidade <= 0)
                throw new ArgumentException("A quantidade deve ser maior que zero.");

            var produto = produtos.ObterPorId(item.ProdutoId)
                ?? throw new KeyNotFoundException($"Produto não encontrado: {item.ProdutoId}");
            if (!produto.Ativo)
                throw new ArgumentException($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");

            pedido.Itens.Add(new ItemPedido
            {
                ProdutoId = produto.Id,
                Nome = produto.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco,
            });
        }

        pedidos.Adicionar(pedido);
        return ParaDto(pedido);
    }

    public PedidoDto Obter(Guid id) =>
        ParaDto(pedidos.ObterPorId(id) ?? throw new KeyNotFoundException($"Pedido não encontrado: {id}"));

    public List<PedidoResumoDto> Listar(Guid? clienteId) =>
        [.. pedidos.Listar()
            .Where(p => clienteId is null || p.ClienteId == clienteId)
            .Select(p => new PedidoResumoDto
            {
                Id = p.Id,
                ClienteId = p.ClienteId,
                Status = p.Status.ToString(),
                // Cálculo de total DUPLICADO (também está em ParaDto e no controller).
                Total = p.Itens.Sum(i => i.PrecoUnitario * i.Quantidade),
            })];

    public PedidoDto Confirmar(Guid id)
    {
        var pedido = pedidos.ObterPorId(id) ?? throw new KeyNotFoundException($"Pedido não encontrado: {id}");

        // Regra de transição DUPLICADA (ver Cancelar). Qualquer um pode fazer pedido.Status = ...
        if (pedido.Status != StatusPedido.Created)
            throw new InvalidOperationException($"Transição inválida: {pedido.Status} → {StatusPedido.Confirmed}.");
        pedido.Status = StatusPedido.Confirmed;

        pedidos.Atualizar(pedido);
        return ParaDto(pedido);
    }

    public PedidoDto Cancelar(Guid id)
    {
        var pedido = pedidos.ObterPorId(id) ?? throw new KeyNotFoundException($"Pedido não encontrado: {id}");

        if (pedido.Status == StatusPedido.Completed)
            throw new InvalidOperationException("Pedido concluído não pode ser cancelado.");
        if (pedido.Status != StatusPedido.Created)
            throw new InvalidOperationException($"Transição inválida: {pedido.Status} → {StatusPedido.Cancelled}.");
        pedido.Status = StatusPedido.Cancelled;

        pedidos.Atualizar(pedido);
        return ParaDto(pedido);
    }

    internal static PedidoDto ParaDto(Pedido pedido) => new()
    {
        Id = pedido.Id,
        ClienteId = pedido.ClienteId,
        Status = pedido.Status.ToString(),
        Total = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade),
        Itens = [.. pedido.Itens.Select(i => new ItemPedidoRespostaDto
        {
            ProdutoId = i.ProdutoId,
            Nome = i.Nome,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario,
            Subtotal = i.PrecoUnitario * i.Quantidade,
        })],
    };
}
