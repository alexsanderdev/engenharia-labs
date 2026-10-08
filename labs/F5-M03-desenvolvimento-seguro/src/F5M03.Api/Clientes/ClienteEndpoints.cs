using F5M03.Api.Dominio;

namespace F5M03.Api.Clientes;

// VULNERÁVEL — corrija (Passos 1, 2, 3 e 5 do Lab).
public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/clientes");

        grupo.MapPost("/", (NovoClienteRequest requisicao, IClienteRepositorio repositorio, TimeProvider relogio, ILoggerFactory loggers) =>
        {
            // VULNERÁVEL: o record inteiro vai para o log (senha e CPF completos).
            loggers.CreateLogger("F5M03.Api.Clientes").LogInformation("Cadastrando cliente {Requisicao}", requisicao);

            // VULNERÁVEL: sem validação; IsAdmin vem do corpo.
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = requisicao.Nome,
                Email = requisicao.Email,
                Cpf = requisicao.Cpf,
                SenhaHash = HashDeSenha.Gerar(requisicao.Senha),
                IsAdmin = requisicao.IsAdmin,
                CriadoEm = relogio.GetUtcNow(),
            };
            repositorio.Adicionar(cliente);

            // VULNERÁVEL: devolve a entidade (SenhaHash, IsAdmin, CPF completo).
            return Results.Created($"/clientes/{cliente.Id}", cliente);
        });

        grupo.MapGet("/{id:guid}", (Guid id, IClienteRepositorio repositorio) =>
            repositorio.Obter(id) is { } cliente ? Results.Ok(cliente) : Results.NotFound());

        return app;
    }
}
