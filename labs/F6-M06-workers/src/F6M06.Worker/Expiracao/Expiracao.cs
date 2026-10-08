using System.ComponentModel.DataAnnotations;
using F6M06.Worker.Pedidos;
using Microsoft.Extensions.Options;

namespace F6M06.Worker.Expiracao;

/// <summary>Configuração do job de expiração (seção "Expiracao"). (PRONTO)</summary>
public sealed class ExpiracaoOptions
{
    public const string Secao = "Expiracao";

    /// <summary>De quanto em quanto tempo o job roda.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Pedido em Created há mais tempo que isso é cancelado por falta de pagamento.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan PrazoDePagamento { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Sem batimento por mais que isso, o health check acusa o worker de travado.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan ToleranciaSemBatimento { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>Quantos ciclos seguidos podem falhar antes de o worker desistir (e o host parar).</summary>
    [Range(1, 1000)]
    public int MaxFalhasConsecutivas { get; set; } = 5;
}

/// <summary>
/// Caso de uso do job: cancela pedidos não pagos. SCOPED — depende do repositório (scoped).
/// </summary>
public sealed class ServicoDeExpiracao(
    IRepositorioDePedidos repositorio,
    TimeProvider relogio,
    IOptions<ExpiracaoOptions> opcoes)
{
    public const string Motivo = "Pagamento não recebido no prazo";

    /// <summary>
    /// Busca os pedidos em Created criados até <c>agora − PrazoDePagamento</c>
    /// (<see cref="IRepositorioDePedidos.ListarCriadosAteAsync"/>), cancela cada um com <see cref="Motivo"/>,
    /// salva e devolve quantos foram cancelados. Pedidos confirmados nunca aparecem (e não poderiam ser cancelados).
    /// </summary>
    public async Task<int> ExpirarAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 2): limite = agora − PrazoDePagamento; ListarCriadosAteAsync(limite); Cancelar(Motivo) em cada um; SalvarAsync; devolva a quantidade");
    }
}
