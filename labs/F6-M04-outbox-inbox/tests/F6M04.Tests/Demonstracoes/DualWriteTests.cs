using F6M04.Pedidos.Aplicacao;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Legado;
using F6M04.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace F6M04.Tests.Demonstracoes;

/// <summary>
/// DEMONSTRAÇÃO (verde desde o início, não altere): os dois jeitos de o "dual write" dar errado. Não existe ordem
/// certa entre "salvar no banco" e "publicar no broker" quando são duas operações independentes.
/// </summary>
[Collection(ColecaoInfra.Nome)]
public sealed class DualWriteTests(InfraFixture infra) : TesteComInfra(infra)
{
    [Fact]
    public async Task DualWrite_BrokerForaDoArDepoisDoCommit_PedidoFicaSalvoEOEventoSePerdeParaSempre()
    {
        await using var escopo = Servicos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
        var servico = new ServicoDePedidosDualWrite(db, new PublicadorForaDoAr(), Relogio, MomentoDaPublicacao.DepoisDoCommit);

        // A requisição falha para o cliente (500)...
        await Should.ThrowAsync<IOException>(() => servico.CriarAsync(new CriarPedido("PED-0001", "ana@cliente.test", 100m)));

        // ...mas o pedido EXISTE. E o evento não está em lugar nenhum: ninguém vai republicar. O e-mail nunca sai,
        // o estoque nunca reserva. (E se o cliente tentar de novo, cria OUTRO pedido.)
        (await ContarPedidosAsync()).ShouldBe(1);
        (await LerOutboxAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DualWrite_PublicaAntesDoCommitEOCommitFalha_OMundoRecebeUmEventoFantasma()
    {
        await using (var escopo = Servicos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
            var primeiro = new ServicoDePedidosDualWrite(db, Publicador, Relogio, MomentoDaPublicacao.DepoisDoCommit);
            await primeiro.CriarAsync(new CriarPedido("PED-0001", "ana@cliente.test", 100m));
        }

        await using (var escopo = Servicos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
            var invertido = new ServicoDePedidosDualWrite(db, Publicador, Relogio, MomentoDaPublicacao.AntesDoCommit);

            // Número repetido: a constraint única derruba o commit DEPOIS da publicação.
            await Should.ThrowAsync<DbUpdateException>(() => invertido.CriarAsync(new CriarPedido("PED-0001", "bia@cliente.test", 200m)));
        }

        Publicador.Publicadas.Count.ShouldBe(2);  // dois eventos "pedido.criado" saíram...
        (await ContarPedidosAsync()).ShouldBe(1); // ...para um pedido só. O segundo é um fantasma.
        Publicador.Publicadas[1].Payload.ShouldContain("bia@cliente.test");
    }
}
