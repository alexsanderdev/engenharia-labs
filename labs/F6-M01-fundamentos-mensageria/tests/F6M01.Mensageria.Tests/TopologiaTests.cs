using F6M01.Mensageria.Tests.Infra;
using F6M01.Mensageria.Topologia;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace F6M01.Mensageria.Tests;

/// <summary>Passo 4 — topologia durável e idempotente (com broker).</summary>
public class TopologiaTests(RabbitFixture rabbit) : RabbitTestBase(rabbit)
{
    [Fact]
    public async Task Declarar_DuasVezes_EhIdempotente()
    {
        await DeclararTopologiaAsync();
        await DeclararTopologiaAsync(); // todo serviço declara o que usa ao subir: não pode quebrar na 2ª vez

        await using var canal = await Conexao.CreateChannelAsync();
        foreach (var fila in TopologiaOrderFlow.Filas)
            (await canal.QueueDeclarePassiveAsync(fila)).QueueName.ShouldBe(fila);
    }

    [Theory]
    [InlineData(TopologiaOrderFlow.FilaReservarEstoque)]
    [InlineData(TopologiaOrderFlow.FilaNotificacaoPedidoCriado)]
    [InlineData(TopologiaOrderFlow.FilaFidelidadeEventosDePedido)]
    public async Task Filas_SaoDuraveis(string fila)
    {
        await DeclararTopologiaAsync();

        // Redeclarar com propriedades diferentes é recusado pelo broker (406 PRECONDITION_FAILED):
        // é assim que provamos, pelo AMQP, que a fila existente é durável.
        var erro = await ReDeclararAsync(c => c.QueueDeclareAsync(fila, durable: false, exclusive: false, autoDelete: false));

        erro.ShouldNotBeNull($"a fila '{fila}' precisa ser DURÁVEL").ShutdownReason!.ReplyCode.ShouldBe((ushort)406);
    }

    [Theory]
    [InlineData(TopologiaOrderFlow.ExchangeComandos, ExchangeType.Direct)]
    [InlineData(TopologiaOrderFlow.ExchangeEventos, ExchangeType.Topic)]
    public async Task Exchanges_TemOTipoCertoESaoDuraveis(string exchange, string tipo)
    {
        await DeclararTopologiaAsync();

        var mesmoTipoDuravel = await ReDeclararAsync(c => c.ExchangeDeclareAsync(exchange, tipo, durable: true, autoDelete: false));
        var outroTipo = await ReDeclararAsync(c => c.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, autoDelete: false));
        var naoDuravel = await ReDeclararAsync(c => c.ExchangeDeclareAsync(exchange, tipo, durable: false, autoDelete: false));

        mesmoTipoDuravel.ShouldBeNull($"'{exchange}' deveria ser {tipo} e durável");
        outroTipo.ShouldNotBeNull($"'{exchange}' não pode ser fanout");
        naoDuravel.ShouldNotBeNull($"'{exchange}' precisa ser DURÁVEL");
    }

    private async Task<OperationInterruptedException?> ReDeclararAsync(Func<IChannel, Task> declarar)
    {
        // Erro de canal fecha o canal: cada tentativa usa um canal descartável.
        await using var canal = await Conexao.CreateChannelAsync();
        try
        {
            await declarar(canal);
            return null;
        }
        catch (OperationInterruptedException ex)
        {
            return ex;
        }
    }
}
