namespace F1M05.Memoria.Tests;

public class ParserCodigoPedidoTests
{
    [Fact]
    public void TryParse_CodigoValido_PreencheTipoAnoESequencial()
    {
        ParserCodigoPedido.TryParse("PED-2026-000123", out var codigo).ShouldBeTrue();

        codigo.ShouldBe(new CodigoPedido(TipoCodigo.Pedido, 2026, 123));
    }

    [Fact]
    public void TryParse_EspacosNasPontasEMinusculas_AceitaDevolucao()
    {
        ParserCodigoPedido.TryParse("  dev-2025-000001 ", out var codigo).ShouldBeTrue();

        codigo.ShouldBe(new CodigoPedido(TipoCodigo.Devolucao, 2025, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PED-2026-00012")]   // curto
    [InlineData("XYZ-2026-000123")]  // prefixo desconhecido
    [InlineData("PED_2026_000123")]  // separador errado
    [InlineData("PED-20A6-000123")]  // ano não numérico
    [InlineData("PED-2026-+00123")]  // sinal não é dígito
    [InlineData("PED-1999-000123")]  // ano fora da faixa
    [InlineData("PED-2026-000000")]  // sequencial zero
    public void TryParse_TextoInvalido_RetornaFalse(string texto)
    {
        ParserCodigoPedido.TryParse(texto, out var codigo).ShouldBeFalse();
        codigo.ShouldBe(default);
    }

    [Fact]
    public void Parse_TextoInvalido_LancaFormatException()
    {
        Should.Throw<FormatException>(() => ParserCodigoPedido.Parse("PED-2026-ABCDEF"));
    }

    [Fact]
    public void TryParse_ValidoOuInvalido_NaoAlocaNoHeap()
    {
        var bytes = Alocacao.Medir(() =>
        {
            _ = ParserCodigoPedido.TryParse(" PED-2026-000123 ", out _);
            _ = ParserCodigoPedido.TryParse("dev-2026-000999", out _);
            _ = ParserCodigoPedido.TryParse("PED-2026-ABCDEF", out _);
            _ = ParserCodigoPedido.TryParse("lixo", out _);
        });

        bytes.ShouldBe(0, "TryParse não pode criar strings intermediárias (Substring, Split, ToUpper...)");
    }

    [Fact]
    public void TryFormat_EscreveNoDestinoComZerosAEsquerda()
    {
        Span<char> destino = stackalloc char[32];

        var ok = ParserCodigoPedido.TryFormat(new CodigoPedido(TipoCodigo.Devolucao, 2026, 42), destino, out var escritos);

        ok.ShouldBeTrue();
        escritos.ShouldBe(15);
        destino[..escritos].ToString().ShouldBe("DEV-2026-000042");
    }

    [Fact]
    public void TryFormat_DestinoPequeno_RetornaFalseSemEscrever()
    {
        var destino = new char[10];

        ParserCodigoPedido.TryFormat(new CodigoPedido(TipoCodigo.Pedido, 2026, 1), destino, out var escritos).ShouldBeFalse();
        escritos.ShouldBe(0);
    }

    [Fact]
    public void TryFormat_NaoAlocaNoHeap()
    {
        var destino = new char[CodigoPedido.Tamanho];
        var codigo = new CodigoPedido(TipoCodigo.Pedido, 2026, 123);

        var bytes = Alocacao.Medir(() => ParserCodigoPedido.TryFormat(codigo, destino, out _));

        bytes.ShouldBe(0);
    }

    [Fact]
    public void ToString_UsaOTryFormat()
    {
        new CodigoPedido(TipoCodigo.Pedido, 2026, 7).ToString().ShouldBe("PED-2026-000007");
    }

    [Fact]
    public void ContarValidos_IgnoraCamposInvalidosEVazios()
    {
        ParserCodigoPedido.ContarValidos("PED-2026-000001;lixo;DEV-2026-000002;;PED-2026-000003").ShouldBe(3);
        ParserCodigoPedido.ContarValidos("PED-2026-000001|PED-2026-000002", '|').ShouldBe(2);
        ParserCodigoPedido.ContarValidos("").ShouldBe(0);
    }

    [Fact]
    public void ContarValidos_NaoAlocaNoHeap()
    {
        var linha = string.Join(';', Enumerable.Range(1, 50).Select(i => $"PED-2026-{i:D6}"));

        var bytes = Alocacao.Medir(() => ParserCodigoPedido.ContarValidos(linha));

        bytes.ShouldBe(0, "use IndexOf + slices em vez de string.Split");
    }

    [Fact]
    public void CodigoPedido_ComoChaveDeHashSet_BuscaSemBoxing()
    {
        // record struct implementa IEquatable<T>: o HashSet compara sem boxing.
        var codigos = new HashSet<CodigoPedido>();
        for (var i = 1; i <= 100; i++)
            codigos.Add(ParserCodigoPedido.Parse($"PED-2026-{i:D6}"));
        var procurado = ParserCodigoPedido.Parse("PED-2026-000050");

        var bytes = Alocacao.Medir(() => codigos.Contains(procurado));

        codigos.Contains(procurado).ShouldBeTrue();
        bytes.ShouldBe(0);
    }
}
