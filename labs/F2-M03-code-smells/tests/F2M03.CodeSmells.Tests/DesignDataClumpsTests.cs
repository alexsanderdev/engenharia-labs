using System.Reflection;
using F2M03.CodeSmells.DataClumps;

namespace F2M03.CodeSmells.Tests;

/// <summary>Design: Endereco, JanelaDeEntrega e o parameter object SolicitacaoDeEntrega. Começam vermelhos.</summary>
public class DesignDataClumpsTests
{
    private static readonly DateOnly Segunda = new(2026, 3, 2);

    [Fact]
    public void EnderecoEJanela_ValidamENormalizamNoConstrutor()
    {
        var endereco = new Endereco("Rua das Flores", "100", "Curitiba", " pr ", "80.010-000");
        endereco.Uf.ShouldBe("PR");
        endereco.Cep.ShouldBe("80010000");
        endereco.CepFormatado.ShouldBe("80010-000");
        Should.Throw<ArgumentException>(() => new Endereco("Rua", "1", "Curitiba", "PRR", "80010000"));
        Should.Throw<ArgumentException>(() => new Endereco("Rua", "1", "Curitiba", "PR", "8001-000"));

        var janela = new JanelaDeEntrega(Segunda, Segunda.AddDays(4));
        janela.Contem(Segunda).ShouldBeTrue();
        janela.Contem(Segunda.AddDays(4)).ShouldBeTrue();
        janela.Contem(Segunda.AddDays(5)).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => new JanelaDeEntrega(Segunda, Segunda.AddDays(-1)));
    }

    [Fact]
    public void Agendar_ComParameterObject_MesmoResultadoDoLegado()
    {
        var solicitacao = new SolicitacaoDeEntrega(
            "Ana",
            new Endereco("Rua das Flores", "100", "Curitiba", "pr", "80.010-000"),
            new JanelaDeEntrega(Segunda, Segunda.AddDays(10)),
            Expressa: false);

        var agenda = new AgendaDeEntregas();

        agenda.Agendar(solicitacao).ShouldBe(new Agendamento(Segunda.AddDays(5), "Ana — Rua das Flores, 100 — Curitiba/PR — CEP 80010-000"));
        agenda.EstimarPrazoEmDias(solicitacao.Endereco, expressa: true).ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => agenda.Agendar(solicitacao with { Janela = new JanelaDeEntrega(Segunda, Segunda.AddDays(4)) }));
    }

    [Fact]
    public void AgendaDeEntregas_MetodosComMaisDe3Parametros_EstaoMarcadosComoObsoletos()
    {
        var metodos = typeof(AgendaDeEntregas).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var longosSemObsolete = metodos
            .Where(m => m.GetParameters().Length > 3 && m.GetCustomAttribute<ObsoleteAttribute>() is null)
            .Select(m => $"{m.Name}({m.GetParameters().Length} parâmetros)")
            .ToList();

        longosSemObsolete.ShouldBeEmpty("Lista longa de parâmetros: crie a versão com objetos e marque a antiga com [Obsolete].");
    }
}
