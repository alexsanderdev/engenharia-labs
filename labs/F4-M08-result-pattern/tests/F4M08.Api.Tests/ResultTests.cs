using F4M08.Api.Resultados;

namespace F4M08.Api.Tests;

/// <summary>Passo 1: Result e Result&lt;T&gt;.</summary>
public sealed class ResultTests
{
    private static readonly Error Erro = Error.NotFound("teste.nao_encontrado", "Não achei.");

    [Fact]
    public void Success_NaoTemErro()
    {
        var resultado = Result.Success();
        var comValor = Result.Success(42);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.IsFailure.ShouldBeFalse();
        resultado.Error.ShouldBe(Error.None);
        comValor.Value.ShouldBe(42);
        comValor.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_GuardaOErro_EFalhaSemErroEhProibida()
    {
        var falha = Result.Failure(Erro);
        var falhaComTipo = Result.Failure<int>(Erro);

        falha.IsFailure.ShouldBeTrue();
        falha.Error.ShouldBe(Erro);
        falhaComTipo.IsSuccess.ShouldBeFalse();
        falhaComTipo.Error.ShouldBe(Erro);
        Should.Throw<ArgumentException>(() => Result.Failure(Error.None));
        Should.Throw<ArgumentException>(() => Result.Failure<int>(Error.None));
    }

    [Fact]
    public void Value_EmFalha_LancaInvalidOperationComOCodigoDoErro()
    {
        var falha = Result.Failure<string>(Erro);

        var ex = Should.Throw<InvalidOperationException>(() => falha.Value);

        ex.Message.ShouldContain("teste.nao_encontrado");
    }

    [Fact]
    public void ConversoesImplicitas_ValorViraSucesso_ErroViraFalha()
    {
        Result<string> sucesso = "ok";
        Result<string> falha = Erro;
        Result semValor = Erro;

        sucesso.IsSuccess.ShouldBeTrue();
        sucesso.Value.ShouldBe("ok");
        falha.IsFailure.ShouldBeTrue();
        falha.Error.ShouldBe(Erro);
        semValor.Error.ShouldBe(Erro);
    }

    [Fact]
    public void MapEBind_TransformamNoSucesso_EPropagamOPrimeiroErroSemExecutarOResto()
    {
        var chamadas = 0;
        Result<int> Dobrar(int x) { chamadas++; return x * 2; }
        Result<int> Recusar(int _) { chamadas++; return Error.Conflict("teste.conflito", "Não pode."); }

        var ok = Result.Success(5).Map(x => x + 1).Bind(Dobrar);
        var falhou = Result.Success(5).Bind(Recusar).Bind(Dobrar).Map(x => x + 1);
        var jaFalhado = Result.Failure<int>(Erro).Map(x => { chamadas += 100; return x; });

        ok.Value.ShouldBe(12);
        falhou.Error.Code.ShouldBe("teste.conflito");
        jaFalhado.Error.ShouldBe(Erro);
        chamadas.ShouldBe(2); // Dobrar (no ok) + Recusar; nada depois do erro foi executado
    }

    [Fact]
    public void Match_ExecutaSoORamoCorrespondente()
    {
        Result.Success(10).Match(v => $"valor {v}", e => e.Code).ShouldBe("valor 10");
        Result.Failure<int>(Erro).Match(v => $"valor {v}", e => e.Code).ShouldBe("teste.nao_encontrado");
        Result.Success().Match(() => "ok", e => e.Code).ShouldBe("ok");
        Result.Failure(Erro).Match(() => "ok", e => e.Code).ShouldBe("teste.nao_encontrado");
    }
}
