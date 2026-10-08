using System.Text;
using F6M05.Sagas.Mensageria;
using RabbitMQ.Client;

namespace F6M05.Tests.Infra;

/// <summary>PRONTA. Utilidades de RabbitMQ para os testes (nomes únicos, publicar, contar, drenar).</summary>
public static class Rabbit
{
    /// <summary>Nome único por teste: os testes rodam em paralelo no mesmo broker sem se ver.</summary>
    public static string NomeUnico(string prefixo) => $"{prefixo}.{Guid.NewGuid():N}";

    /// <summary>Publica um corpo JSON (ou lixo) direto numa fila, pelo exchange padrão, com confirmação.</summary>
    public static async Task PublicarAsync(IConnection conexao, string fila, string corpo, string messageId, string tipo = "Teste")
    {
        await using var canal = await CanalDePublicacao.CriarAsync(conexao);
        var propriedades = new BasicProperties
        {
            MessageId = messageId,
            Type = tipo,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>(),
        };
        await canal.PublicarAsync("", fila, propriedades, Encoding.UTF8.GetBytes(corpo));
    }

    /// <summary>Mensagens prontas na fila (não conta as entregues e ainda sem ack).</summary>
    public static async Task<uint> ContarAsync(IConnection conexao, string fila)
    {
        await using var canal = await conexao.CreateChannelAsync();
        return await canal.MessageCountAsync(fila);
    }

    /// <summary>Retira (com ack) todas as mensagens prontas na fila.</summary>
    public static async Task<IReadOnlyList<MensagemRecebida>> DrenarAsync(IConnection conexao, string fila)
    {
        await using var canal = await conexao.CreateChannelAsync();
        var lidas = new List<MensagemRecebida>();
        while (await canal.BasicGetAsync(fila, autoAck: true) is { } resultado)
            lidas.Add(MensagemRecebida.De(fila, resultado.BasicProperties, resultado.Body, resultado.Redelivered));
        return lidas;
    }

    /// <summary>A fila existe? (declaração passiva num canal descartável: se não existir, o broker fecha o canal).</summary>
    public static async Task<bool> FilaExisteAsync(IConnection conexao, string fila)
    {
        await using var canal = await conexao.CreateChannelAsync();
        try
        {
            await canal.QueueDeclarePassiveAsync(fila);
            return true;
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            return false;
        }
    }
}
