using F2M03.CodeSmells.DataClumps;

namespace F2M03.CodeSmells.Tests;

// Os métodos legados podem ficar [Obsolete] durante a refatoração: o teste de caracterização continua usando-os.
#pragma warning disable CS0618

/// <summary>Caracterização da agenda legada (9 parâmetros). Nunca podem ficar vermelhos.</summary>
public class ComportamentoDataClumpsTests
{
    private static readonly DateOnly Segunda = new(2026, 3, 2);

    [Theory]
    [InlineData("pr", "80.010-000", false, 5, "Ana — Rua das Flores, 100 — Curitiba/PR — CEP 80010-000")]
    [InlineData("SP", "01310100", false, 2, "Ana — Rua das Flores, 100 — Curitiba/SP — CEP 01310-100")]
    [InlineData("ba", "40020-000", true, 1, "Ana — Rua das Flores, 100 — Curitiba/BA — CEP 40020-000 [EXPRESSA]")]
    public void Agendar_DadosValidos_CalculaDataPrevistaEEtiqueta(string uf, string cep, bool expressa, int prazo, string etiqueta)
    {
        var agenda = new AgendaDeEntregas();

        var agendamento = agenda.Agendar("Ana", "Rua das Flores", "100", "Curitiba", uf, cep, Segunda, Segunda.AddDays(10), expressa);

        agendamento.ShouldBe(new Agendamento(Segunda.AddDays(prazo), etiqueta));
        agenda.EstimarPrazoEmDias("Rua das Flores", "100", "Curitiba", uf, cep, expressa).ShouldBe(prazo);
    }

    [Fact]
    public void Agendar_DadosInvalidos_Lancam()
    {
        var agenda = new AgendaDeEntregas();

        Should.Throw<ArgumentException>(() => agenda.Agendar("Ana", "Rua A", "1", "Curitiba", "P", "80010000", Segunda, Segunda.AddDays(9), false));
        Should.Throw<ArgumentException>(() => agenda.Agendar("Ana", "Rua A", "1", "Curitiba", "PR", "8001", Segunda, Segunda.AddDays(9), false));
        Should.Throw<ArgumentException>(() => agenda.Agendar("Ana", "Rua A", "1", "Curitiba", "PR", "80010000", Segunda, Segunda.AddDays(-1), false));
        Should.Throw<InvalidOperationException>(() => agenda.Agendar("Ana", "Rua A", "1", "Curitiba", "PR", "80010000", Segunda, Segunda.AddDays(4), false));
    }
}
