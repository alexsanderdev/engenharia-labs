using System.ComponentModel.DataAnnotations;

namespace MP3.Notificacoes.Worker.Configuracao;

/// <summary>Seção "ServiceBus".</summary>
public sealed class ServiceBusOptions
{
    public const string Secao = "ServiceBus";

    [Required]
    public string Fila { get; set; } = MP3.Contratos.ConvencoesDeMensagem.FilaDeNotificacoes;

    /// <summary>Mensagens processadas em paralelo por instância. A inbox garante que duplicatas concorrentes não geram 2 envios.</summary>
    [Range(1, 64)]
    public int MaxConcorrencia { get; set; } = 4;

    /// <summary>Prefetch 0: com retry por reagendamento e handlers lentos, prefetch alto só segura lock à toa.</summary>
    [Range(0, 500)]
    public int Prefetch { get; set; }
}

/// <summary>Seção "Notificacoes".</summary>
public sealed class NotificacoesOptions
{
    public const string Secao = "Notificacoes";

    /// <summary>Canal usado quando o evento não traz canal preferido (ou traz um que não existe).</summary>
    [Required]
    public string CanalPadrao { get; set; } = "console";

    public RetryOptions Retry { get; set; } = new();
}

public sealed class RetryOptions
{
    /// <summary>Tentativas totais (a 1ª + retries) antes de mandar para a DLQ com "TentativasEsgotadas".</summary>
    [Range(1, 20)]
    public int MaxTentativas { get; set; } = 5;

    public TimeSpan AtrasoBase { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan AtrasoMaximo { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Fração de jitter (0 a 1). 0,2 = ±20% em torno do atraso exponencial.</summary>
    [Range(0.0, 1.0)]
    public double Jitter { get; set; } = 0.2;
}

public enum ModoSmtp
{
    /// <summary>Grava cada e-mail como .eml numa pasta (o "SMTP fake" sem servidor).</summary>
    PastaDeSaida,

    /// <summary>Envia por SMTP de verdade (ex.: smtp4dev em localhost:25 no docker compose).</summary>
    Rede,
}

/// <summary>Seção "Smtp".</summary>
public sealed class SmtpOptions
{
    public const string Secao = "Smtp";

    public ModoSmtp Modo { get; set; } = ModoSmtp.PastaDeSaida;

    public string PastaDeSaida { get; set; } = "emails-enviados";

    public string Host { get; set; } = "localhost";

    public int Porta { get; set; } = 25;

    [Required]
    public string Remetente { get; set; } = "nao-responda@orderflow.local";
}
