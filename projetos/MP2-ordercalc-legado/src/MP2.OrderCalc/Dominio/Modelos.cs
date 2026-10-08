using System.Globalization;

namespace MP2.OrderCalc.Dominio;

public enum TipoCliente { Normal, Vip, Novo }

public enum TipoEntrega { Normal, Expresso, Retirada }

public enum TipoCupom { Desconhecido, Percentual, Valor, FreteGratis }

/// <summary>Erro de validação de negócio. A fachada legada converte em Status = "ERRO".</summary>
public sealed class PedidoInvalidoException(string mensagem) : Exception(mensagem);

public sealed record ProdutoCatalogo(string Sku, Money Preco, double PesoKg, bool IsentoDeImposto, bool Ativo, int Estoque);

public sealed record ClientePedido(string Id, TipoCliente Tipo, bool Bloqueado)
{
    public bool EhVip => Tipo == TipoCliente.Vip;
}

public sealed record CupomPromocional(string Codigo, TipoCupom Tipo, decimal Valor, DateTime Validade, Money MinimoPedido, int UsosRestantes);

/// <summary>Item já validado contra o catálogo.</summary>
public sealed record ItemPedido(ProdutoCatalogo Produto, Quantidade Quantidade)
{
    public Money ValorBruto => Produto.Preco * Quantidade;

    /// <summary>Desconto por volume na linha: 10+ unidades = 5%, 50+ = 10%.</summary>
    public Money DescontoPorVolume => Quantidade.Valor switch
    {
        >= 50 => ValorBruto.Percentual(0.10m),
        >= 10 => ValorBruto.Percentual(0.05m),
        _ => Money.Zero,
    };

    public Money ValorLiquido => ValorBruto - DescontoPorVolume;

    public double PesoKg => Produto.PesoKg * Quantidade.Valor;

    public string Descricao => string.Create(CultureInfo.InvariantCulture, $"{Produto.Sku} x{Quantidade} = {ValorLiquido}");
}
