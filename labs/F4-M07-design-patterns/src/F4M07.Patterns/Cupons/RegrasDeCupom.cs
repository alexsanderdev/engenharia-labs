namespace F4M07.Patterns.Cupons;

/// <summary>
/// As regras de elegibilidade dos cupons do OrderFlow, montadas por COMPOSIÇÃO de especificações.
/// Regra nova do marketing = nova combinação, não um novo <c>if</c> aninhado no checkout.
/// </summary>
public static class RegrasDeCupom
{
    /// <summary>BEMVINDO10: primeira compra E subtotal &gt;= 100 E NÃO contém categoria "Promoções".</summary>
    public static Especificacao<ContextoDoCupom> BemVindo10() =>
        new PrimeiraCompra() & new SubtotalMinimo(100m) & !new ContemCategoria("Promoções");

    /// <summary>FRETEVIP: cliente VIP OU subtotal &gt;= 500.</summary>
    public static Especificacao<ContextoDoCupom> FreteVip() =>
        new ClienteVip() | new SubtotalMinimo(500m);

    /// <summary>BLACKFRIDAY: vigente no período E subtotal &gt;= 200 E (cliente VIP OU NÃO primeira compra).</summary>
    public static Especificacao<ContextoDoCupom> BlackFriday(DateTimeOffset inicio, DateTimeOffset fim) =>
        new Vigente(inicio, fim) & new SubtotalMinimo(200m) & (new ClienteVip() | !new PrimeiraCompra());
}
