namespace MP2.OrderCalc.Tests;

/// <summary>
/// Golden master do legado. Os arquivos .approved.txt foram gerados a partir do código
/// LEGADO, só com o seam de tempo (construtor com TimeProvider). Durante toda a refatoração
/// eles NÃO podem mudar: se mudarem, você mudou comportamento.
/// </summary>
public class CaracterizacaoTests
{
    private static readonly DateTimeOffset Dia23Nov = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Dia30Nov = new(2026, 11, 30, 23, 59, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Dia01Dez = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Sabado = new(2026, 3, 14, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Sexta = new(2026, 3, 13, 18, 0, 0, TimeSpan.Zero);

    public static readonly Caso[] CasosDePedido =
    [
        new("normal-sp-simples", "C1", "MS-002:1", null, "SP", "NORMAL"),
        new("normal-sp-frete-gratis-acima-300", "C1", "TC-003:1", null, "SP", "NORMAL"),
        new("normal-rj-expresso-vale20", "C1", "NB-001:1;CB-008:2", "VALE20", "RJ", "EXPRESSO"),
        new("normal-ba-expresso-prazo-mais-2", "C1", "TC-003:1", null, "BA", "EXPRESSO"),
        new("normal-pr-normal-abaixo-300", "C1", "MS-002:2", null, "PR", "NORMAL"),
        new("vip-mg-frete-gratis", "C2", "NB-001:1", null, "MG", "NORMAL"),
        new("vip-cupom-percentual-maior-substitui", "C2", "TC-003:2", "BEMVINDO10", "SP", "EXPRESSO"),
        new("vip-cupom-valor-acumula", "C2", "TC-003:2", "VALE20", "ES", "EXPRESSO"),
        new("vip-black-friday", "C5", "NB-001:1", null, "RS", "EXPRESSO", Cenarios.BlackFriday),
        new("vip-black-friday-cupom-empata-mantem-vip", "C2", "TC-003:1", "BEMVINDO10", "SP", "NORMAL", Cenarios.BlackFriday),
        new("vip-dia-23-11-sem-black-friday", "C2", "TC-003:1", null, "SP", "NORMAL", Dia23Nov),
        new("vip-dia-30-11-ainda-black-friday", "C2", "TC-003:1", null, "SP", "NORMAL", Dia30Nov),
        new("vip-dia-01-12-acabou", "C2", "TC-003:1", null, "SP", "NORMAL", Dia01Dez),
        new("novo-boas-vindas", "C3", "LV-004:1", null, "SP", "NORMAL"),
        new("novo-abaixo-100-sem-boas-vindas", "C3", "CB-008:2", null, "BA", "NORMAL"),
        new("novo-desconto-tira-do-frete-gratis", "C3", "LV-004:1;CB-008:4", null, "SP", "NORMAL"),
        new("volume-10-unidades-5pct", "C1", "MS-002:10", null, "SP", "NORMAL"),
        new("volume-50-unidades-10pct", "C1", "CB-008:50", null, "PE", "EXPRESSO"),
        new("teto-30pct-mega40", "C1", "NB-001:1", "MEGA40", "SP", "NORMAL"),
        new("teto-30pct-volume-e-mega40", "C1", "MS-002:60", "MEGA40", "SP", "NORMAL"),
        new("cupom-expirado", "C1", "TC-003:1", "EXPIRADO", "SP", "NORMAL"),
        new("cupom-esgotado", "C1", "TC-003:1", "ESGOTADO", "SP", "NORMAL"),
        new("cupom-inexistente-minusculo", "C1", "TC-003:1", " xyz ", "SP", "NORMAL"),
        new("cupom-vazio", "C1", "TC-003:1", "", "SP", "NORMAL"),
        new("cupom-abaixo-do-minimo", "C1", "CB-008:1", "VALE20", "SP", "NORMAL"),
        new("cupom-minusculo-valido", "C1", "TC-003:1", "vale20", "SP", "NORMAL"),
        new("cupom-frete-gratis-no-expresso", "C1", "TC-003:1", "FRETEGRATIS", "AM", "EXPRESSO"),
        new("cupom-frete-gratis-duas-mensagens", "C1", "NB-001:1", "FRETEGRATIS", "SP", "NORMAL"),
        new("vip-cupom-frete", "C2", "TC-003:1", "FRETEGRATIS", "RJ", "EXPRESSO"),
        new("vip-cupom-mega40-teto", "C2", "NB-001:1", "MEGA40", "SP", "NORMAL"),
        new("peso-acima-10kg", "C1", "CD-006:1", null, "BA", "EXPRESSO"),
        new("peso-double-101-mouses", "C1", "MS-002:101", null, "SC", "EXPRESSO"),
        new("livros-isentos-rj", "C1", "LV-004:2;LV-005:1", null, "RJ", "EXPRESSO"),
        new("uf-desconhecida-xx", "C1", "MS-002:1", null, "XX", "NORMAL"),
        new("uf-minuscula", "C1", "MS-002:1", null, "sp", "NORMAL"),
        new("itens-com-espacos-e-vazios", "C1", " ms-002 : 2 ; ; cb-008:1 ", null, "SP", "NORMAL"),
        new("sku-duplicado-estoque-negativo", "C1", "CD-006:2;CD-006:2", null, "SP", "NORMAL"),
        new("vip-arredondamento-bancario-4485-vira-448", "C2", "CB-008:3", null, "MG", "EXPRESSO"),
        new("sabado-prazo", "C1", "MS-002:1", null, "SP", "NORMAL", Sabado),
        new("sexta-expresso-prazo", "C1", "MS-002:1", null, "SP", "EXPRESSO", Sexta),
        // erros: a ORDEM das validações também é comportamento
        new("erro-cliente-vazio", "", "MS-002:1", null, "SP", "NORMAL"),
        new("erro-cliente-nulo", null, "MS-002:1", null, "SP", "NORMAL"),
        new("erro-cliente-inexistente", "C9", "MS-002:1", null, "SP", "NORMAL"),
        new("erro-cliente-bloqueado", "C4", "MS-002:1", null, "SP", "NORMAL"),
        new("erro-uf-curta", "C1", "MS-002:1", null, "S", "NORMAL"),
        new("erro-uf-nula", "C1", "MS-002:1", null, null, "NORMAL"),
        new("erro-frete-minusculo", "C1", "MS-002:1", null, "SP", "normal"),
        new("erro-frete-desconhecido", "C1", "MS-002:1", null, "SP", "DRONE"),
        new("erro-frete-nulo", "C1", "MS-002:1", null, "SP", null),
        new("erro-itens-vazio", "C1", "  ", null, "SP", "NORMAL"),
        new("erro-itens-nulo", "C1", null, null, "SP", "NORMAL"),
        new("erro-itens-so-separadores", "C1", ";;", null, "SP", "NORMAL"),
        new("erro-item-sem-quantidade", "C1", "NB-001", null, "SP", "NORMAL"),
        new("erro-item-com-dois-separadores", "C1", "NB-001:1:2", null, "SP", "NORMAL"),
        new("erro-quantidade-texto", "C1", "NB-001:x", null, "SP", "NORMAL"),
        new("erro-quantidade-zero", "C1", "NB-001:0", null, "SP", "NORMAL"),
        new("erro-quantidade-negativa", "C1", "nb-001:-1", null, "SP", "NORMAL"),
        new("erro-produto-inexistente", "C1", "MS-002:1;XX-999:1", null, "SP", "NORMAL"),
        new("erro-produto-inativo", "C1", "MN-007:1", null, "SP", "NORMAL"),
        new("erro-estoque-insuficiente", "C1", "LV-005:6", null, "SP", "NORMAL"),
        new("erro-produto-do-item-1-vence-quantidade-do-item-2", "C1", "XX-999:1;MS-002:x", null, "SP", "NORMAL"),
        new("erro-cliente-validado-antes-da-uf", "C4", "MS-002:1", null, "S", "DRONE"),
        new("erro-primeiro-item-invalido-vence", "C1", "MS-002:0;MN-007:1", null, "SP", "NORMAL"),
    ];

    public static readonly Caso[] CasosDeOrcamento =
    [
        new("orcamento-normal-sp", "C1", "MS-002:2;LV-004:1", null, "SP", "NORMAL"),
        new("orcamento-vip-black-friday", "C2", "NB-001:1", null, "RS", "EXPRESSO", Cenarios.BlackFriday),
        new("orcamento-vip-normal", "C5", "TC-003:1", null, "MG", "NORMAL"),
        new("orcamento-novo-boas-vindas", "C3", "LV-004:1", null, "SP", "NORMAL"),
        // divergência: no orçamento o frete grátis olha o subtotal ANTES do desconto
        new("orcamento-novo-desconto-nao-tira-frete-gratis", "C3", "LV-004:1;CB-008:4", null, "SP", "NORMAL"),
        new("orcamento-volume-e-teto", "C1", "MS-002:60", null, "SP", "NORMAL"),
        new("orcamento-peso", "C1", "CD-006:1", null, "BA", "EXPRESSO"),
        new("orcamento-livros-rj", "C1", "LV-004:2;LV-005:1", null, "RJ", "EXPRESSO"),
        new("orcamento-sabado", "C1", "MS-002:1", null, "AM", "NORMAL", Sabado),
        // divergência: orçamento não olha bloqueio nem estoque
        new("orcamento-cliente-bloqueado-permitido", "C4", "MS-002:1", null, "SP", "NORMAL"),
        new("orcamento-sem-checar-estoque", "C1", "LV-005:6", null, "SP", "NORMAL"),
        new("orcamento-erro-cliente-vazio", "", "MS-002:1", null, "SP", "NORMAL"),
        new("orcamento-erro-cliente-inexistente", "C9", "MS-002:1", null, "SP", "NORMAL"),
        new("orcamento-erro-uf", "C1", "MS-002:1", null, "SPX", "NORMAL"),
        new("orcamento-erro-frete", "C1", "MS-002:1", null, "SP", "DRONE"),
        new("orcamento-erro-itens", "C1", " ; ", null, "SP", "NORMAL"),
        new("orcamento-erro-item-invalido", "C1", "MS-002", null, "SP", "NORMAL"),
        new("orcamento-erro-quantidade-texto", "C1", "MS-002:um", null, "SP", "NORMAL"),
        new("orcamento-erro-quantidade-zero", "C1", "MS-002:0", null, "SP", "NORMAL"),
        new("orcamento-erro-produto-inexistente", "C1", "XX-999:1", null, "SP", "NORMAL"),
        new("orcamento-erro-produto-inativo", "C1", "MN-007:1", null, "SP", "NORMAL"),
    ];

    [Fact]
    public void Calcular_TodosOsCenarios_BateComGoldenMaster()
    {
        var resultados = CasosDePedido
            .Select(c => Cenarios.Executar(c.Nome, calc => calc.Calcular(c.Cliente!, c.Itens!, c.Cupom!, c.Uf!, c.Frete!), c.Data))
            .ToList();

        Aprovacao.Verificar(resultados);
    }

    [Fact]
    public void CalcularOrcamento_TodosOsCenarios_BateComGoldenMaster()
    {
        var resultados = CasosDeOrcamento
            .Select(c => Cenarios.Executar(c.Nome, calc => calc.CalcularOrcamento(c.Cliente!, c.Itens!, c.Uf!, c.Frete!), c.Data))
            .ToList();

        Aprovacao.Verificar(resultados);
    }
}
