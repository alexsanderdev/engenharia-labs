namespace F2M04.Refatoracao;

/// <summary>Categoria do cliente no programa de fidelidade.</summary>
public enum Categoria
{
    Bronze,
    Prata,
    Ouro,

    /// <summary>Categoria NOVA (regra a implementar no fim do lab).</summary>
    Diamante,
}

/// <summary>Forma de pagamento da compra. <see cref="Outra"/> cobre qualquer valor que o legado não reconhecia.</summary>
public enum FormaDePagamento
{
    Pix,
    Cartao,
    Boleto,
    Outra,
}

/// <summary>Os dados de uma compra que importam para a pontuação (parameter object).</summary>
public sealed record Compra(
    Categoria Categoria,
    decimal Valor,
    FormaDePagamento Pagamento,
    bool PrimeiraCompra,
    bool MesDeAniversario);

/// <summary>
/// A tabela que substitui o if/else por categoria: tudo o que varia entre categorias vira DADO.
/// </summary>
/// <param name="Multiplicador">Pontos por real gasto.</param>
/// <param name="BonusPix">Percentual extra (sobre os pontos base) para pagamento via Pix.</param>
/// <param name="BonusCartao">Percentual extra (sobre os pontos base) para pagamento com cartão.</param>
/// <param name="ValorMinimoAniversario">Valor mínimo da compra para dobrar os pontos no mês de aniversário.</param>
/// <param name="Teto">Máximo de pontos numa compra.</param>
public sealed record RegrasDaCategoria(
    decimal Multiplicador,
    decimal BonusPix,
    decimal BonusCartao,
    decimal ValorMinimoAniversario,
    int Teto)
{
    private static readonly Dictionary<Categoria, RegrasDaCategoria> Tabela = new()
    {
        [Categoria.Bronze] = new(Multiplicador: 1m, BonusPix: 0.10m, BonusCartao: 0m, ValorMinimoAniversario: 200m, Teto: 1_000),
        [Categoria.Prata] = new(Multiplicador: 1.5m, BonusPix: 0.10m, BonusCartao: 0m, ValorMinimoAniversario: 100m, Teto: 3_000),
        [Categoria.Ouro] = new(Multiplicador: 2m, BonusPix: 0.10m, BonusCartao: 0.05m, ValorMinimoAniversario: 0m, Teto: 5_000),
        [Categoria.Diamante] = new(Multiplicador: 3m, BonusPix: 0.10m, BonusCartao: 0.10m, ValorMinimoAniversario: 0m, Teto: 10_000),
    };

    /// <summary>Regras de uma categoria.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Categoria fora do enum.</exception>
    public static RegrasDaCategoria De(Categoria categoria) =>
        Tabela.TryGetValue(categoria, out var regras)
            ? regras
            : throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoria sem regras cadastradas.");

    /// <summary>Percentual de bônus para a forma de pagamento.</summary>
    public decimal BonusPara(FormaDePagamento pagamento) => pagamento switch
    {
        FormaDePagamento.Pix => BonusPix,
        FormaDePagamento.Cartao => BonusCartao,
        _ => 0m,
    };
}

/// <summary>
/// A API nova e limpa do programa de fidelidade. A ordem das regras é a do legado:
/// base + bônus de pagamento → dobra de aniversário → bônus de primeira compra → teto.
/// </summary>
public sealed class ProgramaDeFidelidade
{
    /// <summary>Abaixo deste valor a compra não pontua (só o bônus de boas-vindas).</summary>
    public const decimal ValorMinimoParaPontuar = 50m;

    /// <summary>Bônus fixo na primeira compra que pontua.</summary>
    public const int BonusPrimeiraCompra = 100;

    /// <summary>Bônus de boas-vindas: primeira compra abaixo do mínimo, pagando com qualquer coisa que não seja boleto.</summary>
    public const int BonusBoasVindas = 10;

    /// <summary>Calcula os pontos de uma compra.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Valor negativo.</exception>
    public int CalcularPontos(Compra compra)
    {
        ArgumentNullException.ThrowIfNull(compra);
        ArgumentOutOfRangeException.ThrowIfNegative(compra.Valor);

        if (compra.Valor < ValorMinimoParaPontuar)
            return PontosDeBoasVindas(compra);

        var regras = RegrasDaCategoria.De(compra.Categoria);

        var pontos = PontosBase(compra, regras) + BonusDePagamento(compra, regras);
        if (DobraNoAniversario(compra, regras))
            pontos *= 2;
        if (compra.PrimeiraCompra)
            pontos += BonusPrimeiraCompra;

        return Math.Min(pontos, regras.Teto);
    }

    private static int PontosDeBoasVindas(Compra compra) =>
        compra.PrimeiraCompra && compra.Pagamento != FormaDePagamento.Boleto ? BonusBoasVindas : 0;

    private static int PontosBase(Compra compra, RegrasDaCategoria regras) =>
        (int)Math.Floor(compra.Valor * regras.Multiplicador);

    private static int BonusDePagamento(Compra compra, RegrasDaCategoria regras) =>
        (int)Math.Floor(compra.Valor * regras.Multiplicador * regras.BonusPara(compra.Pagamento));

    private static bool DobraNoAniversario(Compra compra, RegrasDaCategoria regras) =>
        compra.MesDeAniversario && compra.Valor >= regras.ValorMinimoAniversario;
}
