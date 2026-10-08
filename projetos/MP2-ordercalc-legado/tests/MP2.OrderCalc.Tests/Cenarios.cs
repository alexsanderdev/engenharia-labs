using Microsoft.Extensions.Time.Testing;

namespace MP2.OrderCalc.Tests;

/// <summary>
/// Infra dos testes de caracterização: roda um cenário com o Db zerado e o relógio fixo,
/// e devolve o resultado + os efeitos colaterais no "banco" (estoque, cupons, clientes, pedidos).
/// </summary>
public static class Cenarios
{
    /// <summary>Terça-feira, 10/03/2026 10:30. Data neutra: fora da Black Friday, dia útil.</summary>
    public static readonly DateTimeOffset DataPadrao = new(2026, 3, 10, 10, 30, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset BlackFriday = new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Relogio(DateTimeOffset? data = null) => new(data ?? DataPadrao);

    public static object Executar(string nome, Func<OrderCalculator, ResultadoPedido> acao, DateTimeOffset? data = null)
    {
        Db.Reset();
        var estoqueAntes = Db.Produtos.ToDictionary(p => p.Key, p => p.Value.Estoque);
        var usosAntes = Db.Cupons.ToDictionary(c => c.Key, c => c.Value.UsosRestantes);
        var tipoAntes = Db.Clientes.ToDictionary(c => c.Key, c => c.Value.Tipo);

        var resultado = acao(new OrderCalculator(Relogio(data)));

        var efeitos = new
        {
            Estoque = Db.Produtos.Where(p => p.Value.Estoque != estoqueAntes[p.Key])
                .ToDictionary(p => p.Key, p => $"{estoqueAntes[p.Key]} -> {p.Value.Estoque}"),
            UsosDeCupom = Db.Cupons.Where(c => c.Value.UsosRestantes != usosAntes[c.Key])
                .ToDictionary(c => c.Key, c => $"{usosAntes[c.Key]} -> {c.Value.UsosRestantes}"),
            TipoDeCliente = Db.Clientes.Where(c => c.Value.Tipo != tipoAntes[c.Key])
                .ToDictionary(c => c.Key, c => $"{tipoAntes[c.Key]} -> {c.Value.Tipo}"),
            PedidosGravados = Db.Pedidos.Select(p => new { p.Numero, p.ClienteId, p.Total, p.CriadoEm }).ToList(),
            Db.ProximoId,
        };

        return new { Cenario = nome, Resultado = resultado, Efeitos = efeitos };
    }
}

/// <summary>Um cenário de entrada do legado, com primitivos exatamente como a tela antiga manda.</summary>
public sealed record Caso(
    string Nome,
    string? Cliente,
    string? Itens,
    string? Cupom,
    string? Uf,
    string? Frete,
    DateTimeOffset? Data = null);
