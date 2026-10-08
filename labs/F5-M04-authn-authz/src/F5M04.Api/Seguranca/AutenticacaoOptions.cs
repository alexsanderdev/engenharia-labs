using System.ComponentModel.DataAnnotations;

namespace F5M04.Api.Seguranca;

/// <summary>
/// Parâmetros públicos de validação de token (seção <c>Autenticacao</c>). Nada aqui é segredo:
/// emissor e audiência aparecem em todo token, e a chave de VALIDAÇÃO é pública.
/// </summary>
public sealed class AutenticacaoOptions
{
    public const string Secao = "Autenticacao";

    /// <summary>Valor exato esperado no claim <c>iss</c> (inclusive a barra final, se o emissor usar).</summary>
    [Required]
    public string Emissor { get; set; } = "";

    /// <summary>Valor esperado no claim <c>aud</c>: o identificador DESTA API, não do front-end.</summary>
    [Required]
    public string Audiencia { get; set; } = "";

    /// <summary>
    /// Tolerância para diferença de relógio entre emissor e API. O padrão da biblioteca é 5 minutos,
    /// grande demais para tokens de vida curta; aqui usamos 30 segundos.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:02:00")]
    public TimeSpan ToleranciaDeRelogio { get; set; } = TimeSpan.FromSeconds(30);
}
