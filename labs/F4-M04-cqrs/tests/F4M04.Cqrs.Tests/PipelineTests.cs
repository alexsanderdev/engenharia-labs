using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Leitura;
using F4M04.Cqrs.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace F4M04.Cqrs.Tests;

/// <summary>
/// Passos 3 a 5: decorators de logging, validação e unidade de trabalho.
/// A unidade de trabalho real é trocada por um espião que só escreve "Commit" no log.
/// </summary>
public sealed class PipelineTests
{
    private static Ambiente CriarAmbiente() => new(
        comTiposDeTeste: true,
        configurar: s => s.Replace(ServiceDescriptor.Scoped<IUnitOfWork, UnitOfWorkEspiao>()));

    [Fact]
    public async Task Pipeline_CommandValido_RodaNaOrdemLogValidacaoHandlerCommitLog()
    {
        await using var ambiente = CriarAmbiente();

        await ambiente.EnviarAsync(new ComandoDeTeste("ok"));

        var mensagens = ambiente.Mensagens();
        mensagens.Count.ShouldBe(5, string.Join(" | ", mensagens));
        mensagens[0].ShouldBe("Executando ComandoDeTeste");
        mensagens[1].ShouldBe("Validando ComandoDeTeste");
        mensagens[2].ShouldBe("Handler de ComandoDeTeste");
        mensagens[3].ShouldBe("Commit");
        mensagens[4].ShouldStartWith("ComandoDeTeste concluído em");
    }

    [Fact]
    public async Task Pipeline_CommandInvalido_CurtoCircuitaSemHandlerNemCommitELogaAFalha()
    {
        await using var ambiente = CriarAmbiente();

        var ex = await Should.ThrowAsync<ValidacaoException>(() => ambiente.EnviarAsync(new ComandoDeTeste("")));

        ex.NomeDaMensagem.ShouldBe(nameof(ComandoDeTeste));
        ex.Erros.ShouldContainKey("Texto");
        ex.Erros["Texto"].ShouldBe(["Texto é obrigatório."]);
        var mensagens = ambiente.Mensagens();
        mensagens.ShouldNotContain("Handler de ComandoDeTeste");
        mensagens.ShouldNotContain("Commit");
        var erro = ambiente.Logs.GetSnapshot().Where(r => r.Level == LogLevel.Error).ShouldHaveSingleItem();
        erro.Message.ShouldBe("ComandoDeTeste falhou");
        erro.Exception.ShouldBeOfType<ValidacaoException>();
    }

    [Fact]
    public async Task Pipeline_HandlerLanca_NaoFazCommitELogaErroComAExcecao()
    {
        await using var ambiente = CriarAmbiente();

        await Should.ThrowAsync<InvalidOperationException>(() => ambiente.EnviarAsync(new ComandoDeTeste("explodir")));

        ambiente.Mensagens().ShouldContain("Handler de ComandoDeTeste");
        ambiente.Mensagens().ShouldNotContain("Commit");
        ambiente.Logs.GetSnapshot().Single(r => r.Level == LogLevel.Error).Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Pipeline_Query_PassaPorLogMasNuncaPelaUnidadeDeTrabalho()
    {
        await using var ambiente = CriarAmbiente();

        var resposta = await ambiente.ConsultarAsync(new QueryDeTeste());

        resposta.ShouldBe(42);
        var mensagens = ambiente.Mensagens();
        mensagens.Count.ShouldBe(3, string.Join(" | ", mensagens));
        mensagens[0].ShouldBe("Executando QueryDeTeste");
        mensagens[1].ShouldBe("Handler de QueryDeTeste");
        mensagens[2].ShouldStartWith("QueryDeTeste concluído em");
    }

    [Fact]
    public async Task Pipeline_QueryInvalida_TambemEValidadaAntesDoHandler()
    {
        await using var ambiente = CriarAmbiente();

        var ex = await Should.ThrowAsync<ValidacaoException>(() => ambiente.ConsultarAsync(new ListarPedidosDoCliente(Guid.Empty)));

        ex.Erros.Keys.ShouldBe(["ClienteId"]);
    }

    [Fact]
    public async Task Pipeline_CriarPedidoInvalido_AgrupaTodosOsErrosPorPropriedade()
    {
        await using var ambiente = CriarAmbiente();

        var semNada = await Should.ThrowAsync<ValidacaoException>(() => ambiente.EnviarAsync(new CriarPedido(Guid.Empty, [])));
        var quantidadeZero = await Should.ThrowAsync<ValidacaoException>(() =>
            ambiente.EnviarAsync(new CriarPedido(Guid.NewGuid(), [new ItemSolicitado(Guid.NewGuid(), 0)])));

        semNada.Erros.Keys.ShouldBe(["ClienteId", "Itens"], ignoreOrder: true);
        quantidadeZero.Erros.Keys.ShouldBe(["Itens[0].Quantidade"]);
        ambiente.Mensagens().ShouldNotContain("Commit");
    }
}
