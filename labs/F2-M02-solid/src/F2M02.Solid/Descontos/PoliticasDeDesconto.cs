namespace F2M02.Solid.Descontos;

/// <summary>
/// Estratégia de desconto (OCP): cupom novo = classe nova, sem editar o switch de ninguém.
/// <para>
/// CONTRATO (LSP) — toda implementação deve respeitar, para qualquer <c>subtotal ≥ 0</c>:
/// <c>0 ≤ CalcularDesconto(subtotal) ≤ subtotal</c>. Quem usa a interface nunca deve precisar
/// saber QUAL política recebeu para não gerar total negativo.
/// </para>
/// </summary>
public interface IPoliticaDeDesconto
{
    /// <summary>Código do cupom (ex.: "BLACKFRIDAY"). Vazio para "sem desconto".</summary>
    string Cupom { get; }

    decimal CalcularDesconto(decimal subtotal);
}

/// <summary>Null Object: evita <c>if (cupom is null)</c> espalhado pelo código. (Pronta — use como modelo.)</summary>
public sealed class SemDesconto : IPoliticaDeDesconto
{
    public static readonly SemDesconto Instancia = new();

    public string Cupom => "";

    public decimal CalcularDesconto(decimal subtotal) => 0m;
}

/// <summary>Percentual sobre o subtotal, arredondado para 2 casas (AwayFromZero).</summary>
public sealed class DescontoPercentual : IPoliticaDeDesconto
{
    private readonly decimal _percentual;

    /// <param name="percentual">Entre 0 e 1 (0,20 = 20%). Fora disso: <see cref="ArgumentOutOfRangeException"/>.</param>
    public DescontoPercentual(string cupom, decimal percentual)
    {
        // TODO: valide o percentual (ArgumentOutOfRangeException.ThrowIfNegative / ThrowIfGreaterThan).
        Cupom = cupom;
        _percentual = percentual;
    }

    public string Cupom { get; }

    public decimal CalcularDesconto(decimal subtotal) =>
        throw new NotImplementedException($"TODO: subtotal × {_percentual}, arredondado para 2 casas (AwayFromZero) — veja o case BLACKFRIDAY do legado");
}

/// <summary>Percentual com teto em reais (ex.: PRIMEIRACOMPRA = 10% limitado a R$ 50).</summary>
public sealed class DescontoPercentualComTeto(string cupom, decimal percentual, decimal teto) : IPoliticaDeDesconto
{
    public string Cupom => cupom;

    public decimal CalcularDesconto(decimal subtotal) =>
        throw new NotImplementedException($"TODO: o menor entre {percentual:P0} do subtotal e {teto} (dica: reaproveite DescontoPercentual por composição)");
}

/// <summary>
/// Valor fixo (ex.: BEMVINDO30 = R$ 30). Para honrar o contrato (LSP), nunca devolve mais que o subtotal:
/// um pedido de R$ 20 ganha R$ 20, não R$ 30 (o que geraria total negativo).
/// </summary>
public sealed class DescontoFixo(string cupom, decimal valor) : IPoliticaDeDesconto
{
    public string Cupom => cupom;

    public decimal CalcularDesconto(decimal subtotal) =>
        throw new NotImplementedException($"TODO: desconto de {valor}, limitado ao subtotal (contrato LSP)");
}

/// <summary>
/// Resolve o cupom digitado para a política correspondente. Classe CONCRETA, sem interface:
/// a variação está nas políticas, não no catálogo.
/// </summary>
public sealed class CatalogoDePoliticasDeDesconto
{
    private readonly IReadOnlyList<IPoliticaDeDesconto> _politicas;

    public CatalogoDePoliticasDeDesconto(IEnumerable<IPoliticaDeDesconto> politicas)
    {
        // Dica: troque por um Dictionary<string, IPoliticaDeDesconto> com StringComparer.OrdinalIgnoreCase.
        _politicas = [.. politicas];
    }

    /// <summary>Os cupons que o legado conhecia: BLACKFRIDAY (20%), PRIMEIRACOMPRA (10% até R$ 50), BEMVINDO30 (R$ 30).</summary>
    public static CatalogoDePoliticasDeDesconto Padrao() => new(
    [
        new DescontoPercentual("BLACKFRIDAY", 0.20m),
        new DescontoPercentualComTeto("PRIMEIRACOMPRA", 0.10m, 50m),
        new DescontoFixo("BEMVINDO30", 30m),
    ]);

    /// <summary>
    /// Cupom nulo/vazio/espaços → <see cref="SemDesconto"/>. Busca sem diferenciar maiúsculas e ignorando espaços nas pontas.
    /// Cupom desconhecido → <see cref="Dominio.PedidoInvalidoException"/> "Cupom inválido: {cupom}".
    /// </summary>
    public IPoliticaDeDesconto Obter(string? cupom)
    {
        _ = _politicas;
        throw new NotImplementedException("TODO: substitua o switch do legado por uma busca no catálogo (OCP)");
    }
}
