namespace F4M07.Patterns.Legado;

/// <summary>
/// ANTI-PADRÃO: Singleton estático e MUTÁVEL. Estado global compartilhado por todos os testes e requisições,
/// impossível ter duas configurações ao mesmo tempo, e a dependência fica escondida dentro dos métodos.
/// Depois do lab, NADA fora da pasta Legado pode usar esta classe (há um teste de arquitetura para isso).
/// Quando terminar, você pode apagar a pasta Legado inteira.
/// </summary>
public sealed class ConfiguracaoGlobal
{
    private static readonly Lazy<ConfiguracaoGlobal> _instancia = new(() => new ConfiguracaoGlobal());

    private ConfiguracaoGlobal() { }

    public static ConfiguracaoGlobal Instancia => _instancia.Value;

    /// <summary>"Alguém" muda isso em runtime e todo mundo sente.</summary>
    public decimal PercentualDeTaxaDeServico { get; set; } = 0.05m;
}
