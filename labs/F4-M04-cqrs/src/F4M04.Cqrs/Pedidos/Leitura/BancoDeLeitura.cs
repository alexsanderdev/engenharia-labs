using System.Collections.Concurrent;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>
/// Read model: um registro achatado, no formato exato da tela "Meus pedidos".
/// Não tem comportamento nem invariantes; é só dado pronto para exibir.
/// </summary>
public sealed record PedidoResumo(
    Guid Id,
    Guid ClienteId,
    string Status,
    decimal Total,
    int QuantidadeItens,
    DateTimeOffset CriadoEm,
    DateTimeOffset? ConfirmadoEm);

/// <summary>
/// "Banco" do lado de LEITURA (singleton), separado do <see cref="Infra.BancoDeEscrita"/>.
/// Só a projeção escreve aqui; os query handlers só leem.
/// Em produção poderia ser uma tabela desnormalizada, uma view indexada, um Redis ou um índice de busca.
/// </summary>
public sealed class BancoDeLeitura
{
    public ConcurrentDictionary<Guid, PedidoResumo> Pedidos { get; } = new();
}
