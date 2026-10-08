using Azure.Core;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Conexao;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 2 — como o cliente se conecta. Sem rede: o <see cref="ServiceBusClient"/> só abre a conexão
/// AMQP no primeiro envio/recebimento, então dá para verificar a escolha do modo sem broker.
/// </summary>
public sealed class Passo2_ConexaoTests
{
    /// <summary>
    /// Credencial de mentira (no lugar do DefaultAzureCredential): se alguém tentar pegar token, o teste explode.
    /// </summary>
    private sealed class CredencialQueNuncaEhUsada : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("O teste não deveria abrir conexão.");

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("O teste não deveria abrir conexão.");
    }

    [Fact]
    public async Task Criar_ConnectionStringNoDevEManagedIdentityEmProducao_EscolheOModoCerto()
    {
        var opcoes = FabricaDeClienteServiceBus.CriarOpcoesDoCliente();
        opcoes.TransportType.ShouldBe(ServiceBusTransportType.AmqpTcp);
        opcoes.RetryOptions.Mode.ShouldBe(ServiceBusRetryMode.Exponential);
        opcoes.RetryOptions.MaxRetries.ShouldBe(3);

        await using var dev = FabricaDeClienteServiceBus.Criar(new ConexaoServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
        });
        dev.FullyQualifiedNamespace.ShouldBe("localhost");

        await using var producao = FabricaDeClienteServiceBus.Criar(
            new ConexaoServiceBusOptions { FullyQualifiedNamespace = "orderflow-prd.servicebus.windows.net" },
            new CredencialQueNuncaEhUsada());
        producao.FullyQualifiedNamespace.ShouldBe("orderflow-prd.servicebus.windows.net");
        producao.TransportType.ShouldBe(ServiceBusTransportType.AmqpTcp);
    }

    [Fact]
    public void Criar_ConfiguracaoAmbiguaOuIncompleta_LancaInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() => FabricaDeClienteServiceBus.Criar(new ConexaoServiceBusOptions()));

        Should.Throw<InvalidOperationException>(() => FabricaDeClienteServiceBus.Criar(
            new ConexaoServiceBusOptions { FullyQualifiedNamespace = "orderflow-prd.servicebus.windows.net" }),
            "namespace sem credencial não tem como autenticar");

        Should.Throw<InvalidOperationException>(() => FabricaDeClienteServiceBus.Criar(
            new ConexaoServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=a;SharedAccessKey=b;UseDevelopmentEmulator=true;",
                FullyQualifiedNamespace = "orderflow-prd.servicebus.windows.net",
            },
            new CredencialQueNuncaEhUsada()),
            "os dois preenchidos é configuração ambígua: falhe cedo");
    }
}
