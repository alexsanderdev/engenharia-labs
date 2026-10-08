using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace F6M05.Sagas.Saga;

/// <summary>Como uma mensagem foi tratada pelo orquestrador.</summary>
public enum TipoDeResultado
{
    /// <summary>Criou a saga (<see cref="PedidoCriado"/>).</summary>
    Iniciada,

    /// <summary>Mudou o estado e publicou comandos.</summary>
    Aplicada,

    /// <summary>MessageId já processado: estado intocado; comandos do estado atual republicados (mesmos ids).</summary>
    Duplicada,

    /// <summary>Não faz sentido no estado atual (atrasada/fora de ordem): estado intocado, nada publicado.</summary>
    Ignorada,
}

/// <summary>Resultado do processamento de uma mensagem pela saga.</summary>
public sealed record ResultadoSaga(TipoDeResultado Tipo, StatusSaga Status, int Versao, IReadOnlyList<ComandoSaga> ComandosPublicados);

/// <summary>
/// O orquestrador (process manager) da saga de pedido: para cada mensagem, carrega o estado,
/// deduplica, aplica a transição, grava com concorrência otimista e publica os comandos.
/// </summary>
/// <remarks>
/// Ordem importa: <b>grava o estado, depois publica</b>. Se cair entre os dois, o broker reentrega
/// a mensagem; ela é reconhecida como duplicada e os comandos do estado atual são republicados com
/// os mesmos MessageIds (o participante deduplica). Publicar antes de gravar poderia mandar um
/// comando que um conflito de concorrência depois "desfaz" (ex.: confirmar um pedido que o
/// timeout acabou de cancelar). Em produção, o caminho mais robusto é o Outbox (módulo 6.04):
/// estado e comandos na mesma transação.
/// </remarks>
public sealed class OrquestradorSagaPedido(
    IRepositorioDeSagas repositorio,
    IPublicadorDeComandos publicador,
    TimeProvider tempo,
    OpcoesDaSaga? opcoes = null,
    ILogger<OrquestradorSagaPedido>? logger = null)
{
    private readonly OpcoesDaSaga _opcoes = opcoes ?? new OpcoesDaSaga();
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    /// <summary>
    /// Processa uma mensagem da saga.
    /// <list type="number">
    /// <item>Carrega a saga. Se não existe: <see cref="PedidoCriado"/> → <see cref="SagaPedido.Iniciar"/>,
    /// <see cref="IRepositorioDeSagas.InserirAsync"/>, publica → <see cref="TipoDeResultado.Iniciada"/>;
    /// qualquer outra mensagem → <see cref="SagaNaoEncontradaException"/> (transitória: pode ser fora de ordem).</item>
    /// <item>MessageId já processado → republica <see cref="SagaPedido.ComandosDoEstadoAtual"/> → <see cref="TipoDeResultado.Duplicada"/>.</item>
    /// <item><see cref="SagaPedido.Aplicar"/> devolveu <c>null</c> → <see cref="TipoDeResultado.Ignorada"/> (nada gravado/publicado).</item>
    /// <item>Senão <see cref="IRepositorioDeSagas.AtualizarAsync"/> e publica → <see cref="TipoDeResultado.Aplicada"/>.</item>
    /// </list>
    /// Em <see cref="ConflitoDeConcorrenciaException"/> (no inserir ou no atualizar), recomeça do
    /// passo 1 com o estado novo, até <see cref="OpcoesDaSaga.TentativasEmConflito"/> vezes; depois, propaga
    /// (é transitória: o consumidor faz retry).
    /// </summary>
    public Task<ResultadoSaga> ProcessarAsync(MensagemSaga mensagem, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        // TODO (Passo 7): separe "uma tentativa" (carregar → deduplicar → aplicar → gravar → publicar)
        // num método privado e chame-o num laço que captura ConflitoDeConcorrenciaException enquanto
        // tentativa < _opcoes.TentativasEmConflito (Log.ConflitoNaSaga). Agora vem de tempo.GetUtcNow().
        // Logs prontos em Log.cs: MensagemDuplicada, MensagemIgnorada, Transicao.
        // ORDEM: grave o estado ANTES de publicar (veja o remarks da classe).
        throw new NotImplementedException("TODO: carregue, deduplique, aplique, grave (otimista) e publique (Passo 7).");
    }

    /// <summary>PRONTO. Publica os comandos em ordem.</summary>
    private async Task PublicarAsync(IReadOnlyList<ComandoSaga> comandos, CancellationToken ct)
    {
        foreach (var comando in comandos)
            await publicador.PublicarAsync(comando, ct);
    }
}
