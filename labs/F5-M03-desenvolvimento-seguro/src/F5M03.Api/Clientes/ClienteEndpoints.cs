using F5M03.Api.Dominio;
using F5M03.Api.Seguranca;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M03.Api.Clientes;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/clientes");
        grupo.MapPost("/", Cadastrar);
        grupo.MapGet("/{id:guid}", Obter);
        return app;
    }

    internal static Results<Created<ClienteResponse>, ValidationProblem> Cadastrar(
        CadastrarClienteRequest requisicao, IClienteRepositorio repositorio, TimeProvider relogio, ILoggerFactory loggers)
    {
        var erros = ValidadorDeCliente.Validar(requisicao);
        if (!erros.Vazio) return TypedResults.ValidationProblem(erros.ParaDicionario());

        // Mapeamento explícito: cada campo da entidade vem de um lugar conhecido.
        // IsAdmin nasce false; Id e CriadoEm são do servidor.
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nome = requisicao.Nome!.Trim(),
            Email = requisicao.Email!,
            Cpf = ValidadorDeCliente.SomenteDigitos(requisicao.Cpf),
            SenhaHash = HashDeSenha.Gerar(requisicao.Senha!),
            IsAdmin = false,
            CriadoEm = relogio.GetUtcNow(),
        };
        repositorio.Adicionar(cliente);

        // Log útil para operação, sem senha e com dados pessoais mascarados.
        loggers.CreateLogger("F5M03.Api.Clientes").LogInformation(
            "Cliente {ClienteId} cadastrado. E-mail {Email}, CPF {Cpf}",
            cliente.Id, Mascaramento.Email(cliente.Email), Mascaramento.Cpf(cliente.Cpf));

        return TypedResults.Created($"/clientes/{cliente.Id}", ParaResposta(cliente));
    }

    internal static Results<Ok<ClienteResponse>, NotFound> Obter(Guid id, IClienteRepositorio repositorio) =>
        repositorio.Obter(id) is { } cliente ? TypedResults.Ok(ParaResposta(cliente)) : TypedResults.NotFound();

    private static ClienteResponse ParaResposta(Cliente c) =>
        new(c.Id, c.Nome, c.Email, Mascaramento.Cpf(c.Cpf));
}
