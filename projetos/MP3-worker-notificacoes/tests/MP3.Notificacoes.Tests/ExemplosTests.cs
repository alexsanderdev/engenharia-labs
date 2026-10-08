using Azure.Messaging.ServiceBus;
using Microsoft.Data.SqlClient;
using MP3.Contratos;
using MP3.Notificacoes.Tests.Infra;

namespace MP3.Notificacoes.Tests;

/// <summary>
/// Exemplos que já passam: provam que o ambiente (emulador do Service Bus + SQL Server) está de pé
/// e mostram o formato da mensagem. Os testes da SUA missão vêm depois (veja o README).
/// </summary>
[Collection(ColecaoAmbiente.Nome)]
public sealed class ExemplosTests(AmbienteFixture ambiente)
{
    [Fact]
    public async Task ServiceBus_PedidoCriadoPublicadoComoAApiFaz_ChegaNaFilaDeNotificacoes()
    {
        await ambiente.DrenarAsync();
        var evento = Novo.Pedido();

        await using (var sender = ambiente.Cliente.CreateSender(ConvencoesDeMensagem.FilaDeNotificacoes))
            await sender.SendMessageAsync(Novo.Mensagem(evento));

        await using var receiver = ambiente.Cliente.CreateReceiver(ConvencoesDeMensagem.FilaDeNotificacoes);
        var recebida = await receiver.ReceiveMessageAsync(Esperas.Padrao);

        recebida.ShouldNotBeNull("a mensagem não chegou");
        recebida.MessageId.ShouldBe(evento.EventoId.ToString());
        recebida.Subject.ShouldBe(nameof(PedidoCriado));
        recebida.DeliveryCount.ShouldBe(1);
        ConvencoesDeMensagem.Desserializar(recebida.Body.ToMemory().Span).ShouldBe(evento);
        await receiver.CompleteMessageAsync(recebida);
    }

    [Fact]
    public async Task SqlServer_BancoDaInboxEstaDisponivel()
    {
        await using var conexao = new SqlConnection(ambiente.Inbox);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("SELECT DB_NAME()", conexao);

        (await comando.ExecuteScalarAsync()).ShouldBe(AmbienteFixture.BancoDaInbox);
    }
}
