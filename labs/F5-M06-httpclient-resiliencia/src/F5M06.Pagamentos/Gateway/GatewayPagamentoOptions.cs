using System.ComponentModel.DataAnnotations;

namespace F5M06.Pagamentos.Gateway;

/// <summary>
/// Configuração do cliente do gateway de pagamento (seção <c>GatewayPagamento</c> do appsettings).
/// Tempos e limites são REQUISITOS de integração: cada valor deve ter um motivo (SLA do gateway,
/// SLO do checkout), não um número mágico.
/// </summary>
public sealed class GatewayPagamentoOptions
{
    public const string Secao = "GatewayPagamento";

    /// <summary>URL base do gateway (termine com "/" para os caminhos relativos funcionarem).</summary>
    [Required]
    public Uri? BaseAddress { get; set; }

    /// <summary>Chave de API do gateway. Vem de secret store/variável de ambiente, nunca do appsettings versionado.</summary>
    [Required]
    public string ApiKey { get; set; } = "";

    /// <summary>Quanto UMA tentativa pode durar antes de ser abandonada.</summary>
    public TimeSpan TimeoutPorTentativa { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Orçamento total da operação, somando tentativas e esperas entre elas.</summary>
    public TimeSpan TimeoutTotal { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Quantas vezes REPETIR após a primeira tentativa (total de tentativas = 1 + MaxRetentativas).</summary>
    [Range(1, 10)]
    public int MaxRetentativas { get; set; } = 3;

    /// <summary>Atraso base do backoff exponencial (com jitter).</summary>
    public TimeSpan AtrasoBase { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Teto para o atraso entre tentativas.</summary>
    public TimeSpan AtrasoMaximo { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Proporção de falhas (0..1] na janela que abre o circuito.</summary>
    [Range(0.01, 1.0)]
    public double CircuitoTaxaDeFalhas { get; set; } = 0.5;

    /// <summary>Mínimo de chamadas na janela antes de o circuito poder abrir.</summary>
    [Range(2, int.MaxValue)]
    public int CircuitoVazaoMinima { get; set; } = 10;

    /// <summary>Janela de amostragem do circuit breaker.</summary>
    public TimeSpan CircuitoJanela { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Quanto tempo o circuito fica aberto antes de deixar passar uma chamada de teste (meio-aberto).</summary>
    public TimeSpan CircuitoDuracaoAberto { get; set; } = TimeSpan.FromSeconds(15);
}
