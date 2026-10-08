using System.Globalization;
using MP3.Contratos;

namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>Monta o texto e escolhe o destino conforme o canal.</summary>
public static class ModeloDeMensagem
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static Notificacao Para(PedidoCriado evento, string canal)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentNullException.ThrowIfNull(canal);
        var destino = canal.ToUpperInvariant() switch
        {
            "EMAIL" => string.IsNullOrWhiteSpace(evento.Email)
                ? throw new FalhaPermanenteException($"Cliente {evento.ClienteId} sem e-mail para o canal email.")
                : evento.Email,
            "SMS" => string.IsNullOrWhiteSpace(evento.Telefone)
                ? throw new FalhaPermanenteException($"Cliente {evento.ClienteId} sem telefone para o canal sms.")
                : evento.Telefone,
            _ => evento.NomeDoCliente,
        };

        var numero = evento.PedidoId.ToString("N")[..8].ToUpperInvariant();
        return new Notificacao(
            evento.EventoId,
            evento.PedidoId,
            canal,
            destino,
            $"Recebemos o seu pedido {numero}",
            string.Format(PtBr, "Olá, {0}! Recebemos o seu pedido {1} no valor de {2:C}. Avisaremos quando ele for confirmado.",
                evento.NomeDoCliente, numero, evento.ValorTotal));
    }
}
