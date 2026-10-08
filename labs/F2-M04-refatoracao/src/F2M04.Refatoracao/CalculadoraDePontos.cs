namespace F2M04.Refatoracao;

/// <summary>
/// LEGADO do OrderFlow: pontos de fidelidade por compra. Escrito "às pressas" há anos, nunca teve teste.
/// Condicionais aninhadas, números mágicos, strings no lugar de tipos e o mesmo bloco copiado três vezes.
/// O contrato público (assinatura e resultados) NÃO pode mudar: o teste de aprovação garante.
/// </summary>
public sealed class CalculadoraDePontos
{
    /// <summary>
    /// Calcula os pontos de uma compra.
    /// </summary>
    /// <param name="categoria">"BRONZE", "PRATA" ou "OURO" (sem diferenciar maiúsculas; desconhecida = Bronze).</param>
    /// <param name="valorPedido">Valor total do pedido, em reais.</param>
    /// <param name="formaPagamento">"PIX", "CARTAO" ou "BOLETO" (desconhecida = sem bônus).</param>
    /// <param name="primeiraCompra">Primeira compra do cliente.</param>
    /// <param name="mesDeAniversario">A compra foi feita no mês de aniversário do cliente.</param>
    /// <exception cref="ArgumentOutOfRangeException">Valor negativo.</exception>
    public int Calcular(string categoria, decimal valorPedido, string formaPagamento, bool primeiraCompra, bool mesDeAniversario)
    {
        if (valorPedido < 0)
            throw new ArgumentOutOfRangeException(nameof(valorPedido), valorPedido, "Valor do pedido não pode ser negativo.");

        var cat = categoria?.Trim().ToUpperInvariant();
        var pag = formaPagamento?.Trim().ToUpperInvariant();
        var pontos = 0;

        if (valorPedido >= 50)
        {
            if (cat == "OURO")
            {
                pontos = (int)Math.Floor(valorPedido * 2);
                if (pag == "PIX")
                {
                    pontos = pontos + (int)Math.Floor(valorPedido * 2 * 0.10m);
                }
                if (pag == "CARTAO")
                {
                    pontos = pontos + (int)Math.Floor(valorPedido * 2 * 0.05m);
                }
                if (mesDeAniversario)
                {
                    pontos = pontos * 2;
                }
                if (primeiraCompra)
                {
                    pontos = pontos + 100;
                }
                if (pontos > 5000)
                {
                    pontos = 5000;
                }
            }
            else if (cat == "PRATA")
            {
                pontos = (int)Math.Floor(valorPedido * 1.5m);
                if (pag == "PIX")
                {
                    pontos = pontos + (int)Math.Floor(valorPedido * 1.5m * 0.10m);
                }
                if (mesDeAniversario)
                {
                    if (valorPedido >= 100)
                    {
                        pontos = pontos * 2;
                    }
                }
                if (primeiraCompra)
                {
                    pontos = pontos + 100;
                }
                if (pontos > 3000)
                {
                    pontos = 3000;
                }
            }
            else
            {
                // BRONZE (e qualquer categoria desconhecida, "por segurança")
                pontos = (int)Math.Floor(valorPedido);
                if (pag == "PIX")
                {
                    pontos = pontos + (int)Math.Floor(valorPedido * 0.10m);
                }
                if (mesDeAniversario)
                {
                    if (valorPedido >= 200)
                    {
                        pontos = pontos * 2;
                    }
                }
                if (primeiraCompra)
                {
                    pontos = pontos + 100;
                }
                if (pontos > 1000)
                {
                    pontos = 1000;
                }
            }
        }
        else
        {
            // Compra pequena não pontua... exceto o "bônus de boas-vindas" que o marketing pediu em 2019
            if (primeiraCompra)
            {
                if (pag != "BOLETO")
                {
                    pontos = 10;
                }
            }
        }

        return pontos;
    }
}
