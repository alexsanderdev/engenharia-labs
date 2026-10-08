namespace F6M05.Sagas.Saga;

/// <summary>PRONTO. Persistência do estado da saga com concorrência otimista.</summary>
public interface IRepositorioDeSagas
{
    /// <summary>Carrega a saga (com os MessageIds processados) ou <c>null</c>.</summary>
    Task<SagaPedido?> ObterAsync(Guid pedidoId, CancellationToken ct = default);

    /// <summary>
    /// Grava uma saga NOVA (versão 0 → 1) e registra <paramref name="messageId"/> como processada,
    /// na mesma transação. Se a saga já existir (outra instância criou antes), lança
    /// <see cref="ConflitoDeConcorrenciaException"/>.
    /// </summary>
    Task InserirAsync(SagaPedido saga, string messageId, CancellationToken ct = default);

    /// <summary>
    /// Grava as mudanças SE a versão no banco ainda for <see cref="SagaPedido.Versao"/>, incrementa a
    /// versão e registra <paramref name="messageId"/>, na mesma transação. Se a versão mudou (ou a
    /// mensagem já foi registrada), nada é gravado e lança <see cref="ConflitoDeConcorrenciaException"/>.
    /// </summary>
    Task AtualizarAsync(SagaPedido saga, string messageId, CancellationToken ct = default);

    /// <summary>Ids das sagas em <see cref="StatusSaga.AguardandoPagamento"/> com prazo ≤ <paramref name="agora"/>.</summary>
    Task<IReadOnlyList<Guid>> ListarComPrazoVencidoAsync(DateTimeOffset agora, int maximo = 100, CancellationToken ct = default);
}
