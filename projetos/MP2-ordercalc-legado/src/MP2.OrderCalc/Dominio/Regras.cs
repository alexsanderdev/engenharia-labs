namespace MP2.OrderCalc.Dominio;

/// <summary>Cupom que passou nas validações e o efeito que ele tem no pedido.</summary>
public sealed record CupomAvaliado(CupomPromocional Cupom, Money Desconto, bool FreteGratis);

public static class RegrasDeDesconto
{
    public const decimal Teto = 0.30m;
    private const decimal DescontoVip = 0.05m;
    private const decimal ExtraBlackFridayVip = 0.05m;
    private static readonly Money BoasVindas = Money.Reais(10m);
    private static readonly Money MinimoBoasVindas = Money.Reais(100m);

    public static bool EhSemanaBlackFriday(DateTime data) => data.Month == 11 && data.Day is >= 24 and <= 30;

    public static Money DoCliente(ClientePedido cliente, Money baseDesconto, DateTime agora, ICollection<string> mensagens)
    {
        switch (cliente.Tipo)
        {
            case TipoCliente.Vip:
                var desconto = baseDesconto.Percentual(DescontoVip);
                if (EhSemanaBlackFriday(agora))
                {
                    desconto += baseDesconto.Percentual(ExtraBlackFridayVip);
                    mensagens.Add("Black Friday VIP: +5%");
                }
                return desconto;

            case TipoCliente.Novo when baseDesconto >= MinimoBoasVindas:
                mensagens.Add("Desconto de boas-vindas");
                return BoasVindas;

            default:
                return Money.Zero;
        }
    }

    /// <summary>
    /// Valida o cupom na ordem do legado (existe → validade → usos → mínimo) e calcula o efeito.
    /// Cupom recusado vira mensagem, não erro.
    /// </summary>
    public static CupomAvaliado? AvaliarCupom(string codigo, CupomPromocional? cupom, Money baseDesconto, DateTime agora, ICollection<string> mensagens)
    {
        string? recusa = cupom switch
        {
            null => "Cupom inválido: ",
            _ when cupom.Validade < agora => "Cupom expirado: ",
            _ when cupom.UsosRestantes <= 0 => "Cupom esgotado: ",
            _ when baseDesconto < cupom.MinimoPedido => "Pedido abaixo do mínimo do cupom: ",
            _ => null,
        };
        if (recusa is not null)
        {
            mensagens.Add(recusa + codigo);
            return null;
        }

        return cupom!.Tipo switch
        {
            TipoCupom.Percentual => new CupomAvaliado(cupom, baseDesconto.PontosPercentuais(cupom.Valor), false),
            TipoCupom.Valor => new CupomAvaliado(cupom, Money.Reais(cupom.Valor), false),
            TipoCupom.FreteGratis => new CupomAvaliado(cupom, Money.Zero, true),
            _ => new CupomAvaliado(cupom, Money.Zero, false),
        };
    }

    /// <summary>VIP não acumula cupom percentual: vale o maior (empate mantém o VIP).</summary>
    public static Money CombinarComCupom(ClientePedido cliente, Money descontoCliente, CupomAvaliado? cupom, ICollection<string> mensagens)
    {
        if (cupom is null) return descontoCliente;

        if (cliente.EhVip && cupom.Cupom.Tipo == TipoCupom.Percentual)
        {
            if (cupom.Desconto > descontoCliente)
            {
                mensagens.Add("Cupom substitui desconto VIP");
                return cupom.Desconto;
            }

            mensagens.Add("Desconto VIP mantido (cupom não acumula)");
            return descontoCliente;
        }

        return descontoCliente + cupom.Desconto;
    }

    public static Money AplicarTeto(Money descontoTotal, Money subtotal, ICollection<string> mensagens)
    {
        var teto = subtotal.Percentual(Teto);
        if (descontoTotal <= teto) return descontoTotal;

        mensagens.Add("Desconto limitado a 30%");
        return teto;
    }
}

public static class RegrasDeFrete
{
    private const double PesoIncluso = 10;
    private static readonly Money PorKgExcedente = Money.Reais(2.5m);
    private const decimal FatorExpresso = 1.8m;
    public static readonly Money MinimoFreteGratis = Money.Reais(300m);

    public static Money Calcular(Uf uf, double pesoKg, TipoEntrega entrega)
    {
        if (entrega == TipoEntrega.Retirada) return Money.Zero;

        var frete = Money.Reais(uf switch
        {
            { Sigla: "SP" } => 15m,
            { EhSudeste: true } => 20m,
            { EhSul: true } => 25m,
            _ => 35m,
        });

        if (pesoKg > PesoIncluso)
        {
            var kgExcedentes = (int)Math.Ceiling(pesoKg - PesoIncluso);
            frete += Money.Reais(kgExcedentes * PorKgExcedente.Valor);
        }

        return entrega == TipoEntrega.Expresso ? Money.Reais(frete.Valor * FatorExpresso).Arredondar() : frete;
    }
}

public static class RegrasDeImposto
{
    public static decimal Aliquota(Uf uf) => uf.Sigla switch
    {
        "SP" => 0.18m,
        "RJ" => 0.20m,
        _ => 0.17m,
    };

    public static Money Calcular(Money baseTributavel, Uf uf) => baseTributavel.Percentual(Aliquota(uf));
}

public static class CalendarioDeEntrega
{
    public static int DiasUteis(Uf uf, TipoEntrega entrega) => entrega switch
    {
        TipoEntrega.Retirada => 1,
        TipoEntrega.Expresso => uf.EhSudeste ? 2 : 4,
        _ => uf.EhSudeste ? 5 : 7,
    };

    public static DateTime Prazo(DateTime agora, Uf uf, TipoEntrega entrega)
    {
        var data = agora.Date;
        for (var dias = DiasUteis(uf, entrega); dias > 0;)
        {
            data = data.AddDays(1);
            if (data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) dias--;
        }

        return data;
    }
}
