namespace F6M08.Consistencia.Consistencia;

/// <summary>Seção "Consistencia" do appsettings. Pronto: leia, não altere.</summary>
public sealed class ConsistenciaOptions
{
    public const string Secao = "Consistencia";

    /// <summary>Quanto tempo o comando de criação fica na fila antes de ser processado (o "assíncrono" do 202).</summary>
    public TimeSpan AtrasoDoProcessamento { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Quanto tempo um evento leva da fonte até a projeção (a janela de inconsistência).</summary>
    public TimeSpan AtrasoDaProjecao { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Quanto uma leitura com token de consistência aceita esperar a projeção antes de cair para a fonte.</summary>
    public TimeSpan EsperaMaximaDaLeitura { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Divergências mais novas que isso NÃO são corrigidas pela reconciliação: provavelmente o evento
    /// ainda está "no fio". Deve ser bem maior que o atraso normal da projeção (p99), senão o job
    /// briga com o consumidor.
    /// </summary>
    public TimeSpan ToleranciaDaReconciliacao { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan IntervaloDaReconciliacao { get; set; } = TimeSpan.FromMinutes(1);
}
