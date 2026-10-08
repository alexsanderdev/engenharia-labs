namespace F5M05.Api.Idempotencia;

// ARQUIVO PRONTO — não precisa alterar.
// A interface é assíncrona e "pensa em Redis" de propósito: na Fase 6 dá para trocar o
// armazém em memória por um distribuído (SET chave valor NX PX ... para reservar) sem mexer no middleware.

/// <summary>Configuração (seção "Idempotencia").</summary>
public sealed class IdempotenciaOptions
{
    public const string Secao = "Idempotencia";
    public const string Cabecalho = "Idempotency-Key";
    public const string CabecalhoDeReplay = "Idempotent-Replayed";

    /// <summary>Por quanto tempo uma resposta concluída é guardada para replay (Stripe: 24 h).</summary>
    public TimeSpan Retencao { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Por quanto tempo uma reserva "em andamento" vale. Se o processo morrer no meio, a chave
    /// não pode ficar travada para sempre: depois desse tempo outra tentativa pode reservar.
    /// </summary>
    public TimeSpan TempoMaximoDeProcessamento { get; set; } = TimeSpan.FromSeconds(30);

    public int TamanhoMaximoDaChave { get; set; } = 255;
}

/// <summary>A resposta HTTP guardada para replay (bytes: serializável para Redis).</summary>
public sealed record RespostaArmazenada(int StatusCode, string? ContentType, string? Location, byte[] Corpo);

public enum SituacaoDaReserva
{
    /// <summary>Chave nova (ou expirada): ESTA requisição deve processar.</summary>
    Reservada,

    /// <summary>Outra requisição com a mesma chave está processando agora.</summary>
    EmAndamento,

    /// <summary>Já processada com o mesmo corpo: devolva a resposta guardada.</summary>
    Concluida,

    /// <summary>A chave já foi usada com OUTRO corpo: erro do cliente.</summary>
    CorpoDiferente,
}

public sealed record ResultadoDaReserva(SituacaoDaReserva Situacao, RespostaArmazenada? Resposta = null)
{
    public static readonly ResultadoDaReserva Reservada = new(SituacaoDaReserva.Reservada);
    public static readonly ResultadoDaReserva EmAndamento = new(SituacaoDaReserva.EmAndamento);
    public static readonly ResultadoDaReserva CorpoDiferente = new(SituacaoDaReserva.CorpoDiferente);
    public static ResultadoDaReserva Concluida(RespostaArmazenada resposta) => new(SituacaoDaReserva.Concluida, resposta);
}

public interface IArmazemDeIdempotencia
{
    /// <summary>
    /// Operação ATÔMICA: se a chave não existe (ou expirou), reserva para quem chamou;
    /// senão informa a situação. Duas chamadas simultâneas com a mesma chave nunca recebem ambas "Reservada".
    /// </summary>
    Task<ResultadoDaReserva> TentarReservarAsync(string chave, string hashDaRequisicao, CancellationToken ct);

    /// <summary>Grava a resposta final da chave reservada (passa a valer a retenção).</summary>
    Task ConcluirAsync(string chave, RespostaArmazenada resposta, CancellationToken ct);

    /// <summary>Desfaz a reserva (falha no servidor): o cliente pode tentar de novo com a mesma chave.</summary>
    Task LiberarAsync(string chave, CancellationToken ct);
}

/// <summary>Metadado de endpoint: "este endpoint exige Idempotency-Key".</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ExigeIdempotenciaAttribute : Attribute;

public static class IdempotenciaEndpointExtensions
{
    /// <summary>Marca o endpoint para o <c>IdempotenciaMiddleware</c>.</summary>
    public static TBuilder ExigirIdempotencia<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new ExigeIdempotenciaAttribute());
}
