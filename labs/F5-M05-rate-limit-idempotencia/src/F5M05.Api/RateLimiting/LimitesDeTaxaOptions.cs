namespace F5M05.Api.RateLimiting;

// ARQUIVO PRONTO — não precisa alterar.
// Limites vêm de configuração (seção "LimitesDeTaxa"): ajustar limite em produção
// não pode exigir deploy, e os testes usam valores próprios.

public sealed class LimitesDeTaxaOptions
{
    public const string Secao = "LimitesDeTaxa";

    /// <summary>POST /pedidos — janela fixa por cliente.</summary>
    public JanelaFixaOptions Pedidos { get; set; } = new();

    /// <summary>GET /pedidos/{id} — janela deslizante por cliente.</summary>
    public JanelaDeslizanteOptions Consultas { get; set; } = new();

    /// <summary>GET /catalogo — balde de tokens por IP (anônimo).</summary>
    public BaldeDeTokensOptions Catalogo { get; set; } = new();

    /// <summary>GET /relatorios/vendas — concorrência global.</summary>
    public ConcorrenciaOptions Relatorios { get; set; } = new();
}

public sealed class JanelaFixaOptions
{
    public int Limite { get; set; } = 10;
    public TimeSpan Janela { get; set; } = TimeSpan.FromMinutes(1);
}

public sealed class JanelaDeslizanteOptions
{
    public int Limite { get; set; } = 60;
    public TimeSpan Janela { get; set; } = TimeSpan.FromMinutes(1);
    public int Segmentos { get; set; } = 6;
}

public sealed class BaldeDeTokensOptions
{
    public int Capacidade { get; set; } = 20;
    public int TokensPorPeriodo { get; set; } = 10;
    public TimeSpan Periodo { get; set; } = TimeSpan.FromSeconds(10);
}

public sealed class ConcorrenciaOptions
{
    public int Limite { get; set; } = 1;
    public int Fila { get; set; }
}
