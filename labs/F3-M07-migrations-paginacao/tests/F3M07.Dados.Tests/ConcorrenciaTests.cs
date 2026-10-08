using F3M07.Dados.Concorrencia;
using F3M07.Dados.Modelo;
using F3M07.Dados.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M07.Dados.Tests;

/// <summary>Passos 1, 5 e 6: rowversion e as duas estratégias de concorrência otimista.</summary>
[Collection(ColecaoBancoPrincipal.Nome)]
public sealed class ConcorrenciaTests(SqlServerFixture sql) : BancoPrincipalTestBase(sql)
{
    // ---------- Passo 1: rowversion configurado ----------

    [Fact]
    public async Task DoisContextos_SegundoSalvaComVersaoVelha_LancaDbUpdateConcurrencyException()
    {
        var clienteId = await SemearClienteAsync();
        var semeado = await SemearPedidoAsync(clienteId, 100m);
        semeado.Versao.Length.ShouldBe(8, "Versao deve ser rowversion (8 bytes gerados pelo SQL Server).");

        await using var atendente = NovoContexto();
        await using var gerente = NovoContexto();
        var doAtendente = await atendente.Pedidos.SingleAsync(p => p.Id == semeado.Id, Ct);
        var doGerente = await gerente.Pedidos.SingleAsync(p => p.Id == semeado.Id, Ct);

        doGerente.Total = 90m;
        await gerente.SaveChangesAsync(Ct); // primeiro a salvar ganha; o rowversion muda
        doGerente.Versao.ShouldNotBe(semeado.Versao);

        doAtendente.Status = StatusPedido.Confirmed;
        // Sem token de concorrência, este save sobrescreveria em silêncio ("last write wins").
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => atendente.SaveChangesAsync(Ct));
    }

    // ---------- Passo 5: estratégia "devolver 409" ----------

    [Fact]
    public async Task Cancelar_ComAVersaoAtual_GravaEDevolveNovaVersao()
    {
        var clienteId = await SemearClienteAsync();
        var pedido = await SemearPedidoAsync(clienteId, 100m);

        await using var db = NovoContexto();
        var resultado = await new AlteracaoDePedidos(db).CancelarAsync(pedido.Id, pedido.Versao, Ct);

        var ok = resultado.ShouldBeOfType<ResultadoAlteracao.Ok>();
        ok.NovaVersao.ShouldNotBe(pedido.Versao);
        await using var conferencia = NovoContexto();
        var noBanco = await conferencia.Pedidos.SingleAsync(p => p.Id == pedido.Id, Ct);
        noBanco.Status.ShouldBe(StatusPedido.Cancelled);
        noBanco.Versao.ShouldBe(ok.NovaVersao);
    }

    [Fact]
    public async Task Cancelar_ComVersaoVelha_DevolveConflitoComVersaoAtualENaoGrava()
    {
        var clienteId = await SemearClienteAsync();
        var pedido = await SemearPedidoAsync(clienteId, 100m);
        var versaoQueOClienteLeu = pedido.Versao;

        // Outro usuário confirma o pedido depois que o cliente leu a versão.
        await using (var outro = NovoContexto())
        {
            var p = await outro.Pedidos.SingleAsync(x => x.Id == pedido.Id, Ct);
            p.Status = StatusPedido.Confirmed;
            await outro.SaveChangesAsync(Ct);
        }

        await using var db = NovoContexto();
        var resultado = await new AlteracaoDePedidos(db).CancelarAsync(pedido.Id, versaoQueOClienteLeu, Ct);

        var conflito = resultado.ShouldBeOfType<ResultadoAlteracao.Conflito>();
        await using var conferencia = NovoContexto();
        var noBanco = await conferencia.Pedidos.SingleAsync(p => p.Id == pedido.Id, Ct);
        noBanco.Status.ShouldBe(StatusPedido.Confirmed); // o cancelamento NÃO passou por cima
        conflito.VersaoAtual.ShouldBe(noBanco.Versao);
    }

    [Fact]
    public async Task Cancelar_PedidoInexistente_DevolveNaoEncontrado()
    {
        await using var db = NovoContexto();

        var resultado = await new AlteracaoDePedidos(db).CancelarAsync(999_999, new byte[8], Ct);

        resultado.ShouldBeOfType<ResultadoAlteracao.NaoEncontrado>();
    }

    // ---------- Passo 6: estratégia "recarregar e reaplicar" ----------

    [Fact]
    public async Task AplicarDesconto_ConflitoNaPrimeiraTentativa_RecarregaEReaplicaSobreOValorNovo()
    {
        var clienteId = await SemearClienteAsync();
        var pedido = await SemearPedidoAsync(clienteId, 100m);
        // Entre o SELECT e o UPDATE, alguém adiciona R$ 100 ao total (uma vez).
        var concorrente = new EscritaConcorrenteSimulada(ConnectionString, pedido.Id, acrescimoNoTotal: 100m, vezes: 1);

        await using var db = NovoContexto(concorrente);
        var atualizado = await new AlteracaoDePedidos(db).AplicarDescontoAsync(pedido.Id, 10m, ct: Ct);

        // 10% sobre 200 (valor novo), não sobre 100 (valor velho) — e o acréscimo do outro usuário não se perdeu.
        atualizado.Total.ShouldBe(180m);
        concorrente.ChamadasDeSaveChanges.ShouldBe(2);
        await using var conferencia = NovoContexto();
        (await conferencia.Pedidos.SingleAsync(p => p.Id == pedido.Id, Ct)).Total.ShouldBe(180m);
    }

    [Fact]
    public async Task AplicarDesconto_ConflitoEmTodasAsTentativas_DesisteDepoisDoLimite()
    {
        var clienteId = await SemearClienteAsync();
        var pedido = await SemearPedidoAsync(clienteId, 100m);
        var concorrente = new EscritaConcorrenteSimulada(ConnectionString, pedido.Id, acrescimoNoTotal: 1m, vezes: int.MaxValue);

        await using var db = NovoContexto(concorrente);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => new AlteracaoDePedidos(db).AplicarDescontoAsync(pedido.Id, 10m, maxTentativas: 3, ct: Ct));
        concorrente.ChamadasDeSaveChanges.ShouldBe(3); // nem loop infinito, nem desistir cedo demais
    }
}
