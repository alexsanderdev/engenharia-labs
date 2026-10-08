using F4M08.Api.Http;
using F4M08.Api.Resultados;

namespace F4M08.Api.Tests;

/// <summary>Passos 1 e 3: Error tipado e o mapeamento tipo → status HTTP.</summary>
public sealed class ErrorTests
{
    private static Error Fabricar(ErrorType tipo, string code, string message) => tipo switch
    {
        ErrorType.Validation => Error.Validation(code, message),
        ErrorType.NotFound => Error.NotFound(code, message),
        ErrorType.Conflict => Error.Conflict(code, message),
        ErrorType.Forbidden => Error.Forbidden(code, message),
        _ => Error.Failure(code, message),
    };

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Failure)]
    public void Fabricas_DefinemTipoCodigoEMensagem_ERecusamCodigoVazio(ErrorType tipo)
    {
        var erro = Fabricar(tipo, "pedido.algo", "Mensagem para humanos.");

        erro.Type.ShouldBe(tipo);
        erro.Code.ShouldBe("pedido.algo");
        erro.Message.ShouldBe("Mensagem para humanos.");
        erro.ShouldBe(Fabricar(tipo, "pedido.algo", "Mensagem para humanos.")); // igualdade por valor (record)
        Should.Throw<ArgumentException>(() => Fabricar(tipo, " ", "Mensagem."));
        Should.Throw<ArgumentException>(() => Fabricar(tipo, "pedido.algo", ""));
    }

    [Fact]
    public void Validation_GuardaOsErrosPorCampo()
    {
        var erro = Error.Validation("pedido.validacao", "Campos inválidos.", new Dictionary<string, string[]>
        {
            ["clienteId"] = ["Informe o cliente."],
        });

        erro.ValidationErrors.ShouldNotBeNull()["clienteId"].ShouldBe(["Informe o cliente."]);
        Error.NotFound("x.y", "z").ValidationErrors.ShouldBeNull();
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Failure, 422)]
    public void ToStatusCode_MapeiaCadaTipoParaOStatusHttp(ErrorType tipo, int status)
    {
        tipo.ToStatusCode().ShouldBe(status);
        tipo.ToTitle().ShouldNotBeNullOrWhiteSpace();
    }
}
