using Microsoft.Extensions.Options;

namespace F5M05.Api.Idempotencia;

/// <summary>
/// Armazém em memória (Passo 4). Uma trava simples (<c>Lock</c> + <c>lock</c>) basta: as seções
/// críticas são minúsculas e sem I/O. Use SEMPRE o <see cref="TimeProvider"/> injetado (nunca DateTime.UtcNow):
/// é ele que os testes avançam.
/// </summary>
public sealed class ArmazemDeIdempotenciaEmMemoria(TimeProvider relogio, IOptions<IdempotenciaOptions> opcoes) : IArmazemDeIdempotencia
{
    // Dica: guarde por chave { Hash, Resposta (null enquanto em andamento), ExpiraEm }.
    // Reserva nova expira em TempoMaximoDeProcessamento; ao concluir, passa a expirar em Retencao.
    private readonly TimeProvider _relogio = relogio;
    private readonly IOptions<IdempotenciaOptions> _opcoes = opcoes;

    public Task<ResultadoDaReserva> TentarReservarAsync(string chave, string hashDaRequisicao, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO: sob trava — descarte registro vencido; inexistente → reserva (Reservada); hash diferente → CorpoDiferente; " +
            "sem resposta → EmAndamento; com resposta → Concluida(resposta).");

    public Task ConcluirAsync(string chave, RespostaArmazenada resposta, CancellationToken ct) =>
        throw new NotImplementedException("TODO: grave a resposta e renove a expiração para agora + Retencao.");

    public Task LiberarAsync(string chave, CancellationToken ct) =>
        throw new NotImplementedException("TODO: remova a chave (o cliente poderá tentar de novo).");
}
