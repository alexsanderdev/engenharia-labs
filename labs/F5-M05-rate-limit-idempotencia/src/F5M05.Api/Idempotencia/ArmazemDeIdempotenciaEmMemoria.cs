using Microsoft.Extensions.Options;

namespace F5M05.Api.Idempotencia;

/// <summary>
/// Armazém em memória. Uma trava global simples basta (as seções críticas são minúsculas e sem I/O).
/// Limitações (é o motivo do Redis na Fase 6): não sobrevive a restart, e com 2+ instâncias
/// atrás do balanceador cada uma tem o seu dicionário — a garantia some.
/// </summary>
public sealed class ArmazemDeIdempotenciaEmMemoria(TimeProvider relogio, IOptions<IdempotenciaOptions> opcoes) : IArmazemDeIdempotencia
{
    private sealed class Registro
    {
        public required string Hash { get; init; }
        public RespostaArmazenada? Resposta { get; set; }
        public DateTimeOffset ExpiraEm { get; set; }
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<string, Registro> _registros = new(StringComparer.Ordinal);

    public Task<ResultadoDaReserva> TentarReservarAsync(string chave, string hashDaRequisicao, CancellationToken ct)
    {
        lock (_trava)
        {
            var agora = relogio.GetUtcNow();

            // Expiração preguiçosa: quem encontra um registro vencido o descarta.
            if (_registros.TryGetValue(chave, out var registro) && registro.ExpiraEm <= agora)
            {
                _registros.Remove(chave);
                registro = null;
            }

            if (registro is null)
            {
                _registros[chave] = new Registro
                {
                    Hash = hashDaRequisicao,
                    ExpiraEm = agora + opcoes.Value.TempoMaximoDeProcessamento,
                };
                return Task.FromResult(ResultadoDaReserva.Reservada);
            }

            if (!string.Equals(registro.Hash, hashDaRequisicao, StringComparison.Ordinal))
                return Task.FromResult(ResultadoDaReserva.CorpoDiferente);

            return Task.FromResult(registro.Resposta is { } resposta
                ? ResultadoDaReserva.Concluida(resposta)
                : ResultadoDaReserva.EmAndamento);
        }
    }

    public Task ConcluirAsync(string chave, RespostaArmazenada resposta, CancellationToken ct)
    {
        lock (_trava)
        {
            if (_registros.TryGetValue(chave, out var registro))
            {
                registro.Resposta = resposta;
                registro.ExpiraEm = relogio.GetUtcNow() + opcoes.Value.Retencao;
            }
        }
        return Task.CompletedTask;
    }

    public Task LiberarAsync(string chave, CancellationToken ct)
    {
        lock (_trava)
        {
            _registros.Remove(chave);
        }
        return Task.CompletedTask;
    }
}
