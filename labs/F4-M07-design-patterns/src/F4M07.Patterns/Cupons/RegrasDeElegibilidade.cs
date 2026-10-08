using System.Globalization;

namespace F4M07.Patterns.Cupons;

/// <summary>Categoria do cliente no programa de fidelidade.</summary>
public enum CategoriaDoCliente
{
    Comum,
    Vip,
}

/// <summary>Tudo que as regras de cupom podem olhar. Montado pelo caso de uso a partir do carrinho e do cliente.</summary>
public sealed record ContextoDoCupom(
    decimal Subtotal,
    int PedidosAnterioresDoCliente,
    CategoriaDoCliente CategoriaDoCliente,
    IReadOnlyCollection<string> CategoriasDosItens,
    DateTimeOffset Agora);

/// <summary>Subtotal maior ou igual ao mínimo. Descrição: "subtotal &gt;= {minimo:N2}" (pt-BR, ex.: "subtotal &gt;= 100,00").</summary>
public sealed class SubtotalMinimo(decimal minimo) : Especificacao<ContextoDoCupom>
{
    public override bool EhSatisfeitaPor(ContextoDoCupom candidato) => candidato.Subtotal >= minimo;

    public override string Descricao => $"subtotal >= {minimo.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}";
}

/// <summary>Cliente sem nenhum pedido anterior. Descrição: "primeira compra".</summary>
public sealed class PrimeiraCompra : Especificacao<ContextoDoCupom>
{
    public override bool EhSatisfeitaPor(ContextoDoCupom candidato) => candidato.PedidosAnterioresDoCliente == 0;

    public override string Descricao => "primeira compra";
}

/// <summary>Cliente VIP. Descrição: "cliente VIP".</summary>
public sealed class ClienteVip : Especificacao<ContextoDoCupom>
{
    public override bool EhSatisfeitaPor(ContextoDoCupom candidato) => candidato.CategoriaDoCliente == CategoriaDoCliente.Vip;

    public override string Descricao => "cliente VIP";
}

/// <summary>Algum item é da categoria (sem diferenciar maiúsculas). Descrição: "contém categoria {categoria}".</summary>
public sealed class ContemCategoria(string categoria) : Especificacao<ContextoDoCupom>
{
    public override bool EhSatisfeitaPor(ContextoDoCupom candidato) =>
        candidato.CategoriasDosItens.Contains(categoria, StringComparer.OrdinalIgnoreCase);

    public override string Descricao => $"contém categoria {categoria}";
}

/// <summary>
/// <c>Agora</c> dentro de [inicio, fim) — início inclusivo, fim EXCLUSIVO (evita a "meia-noite" ambígua).
/// Descrição: "vigente".
/// </summary>
public sealed class Vigente(DateTimeOffset inicio, DateTimeOffset fim) : Especificacao<ContextoDoCupom>
{
    public override bool EhSatisfeitaPor(ContextoDoCupom candidato) => candidato.Agora >= inicio && candidato.Agora < fim;

    public override string Descricao => "vigente";
}
