using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;

namespace F4M05.Api.Tests;

/// <summary>
/// TESTES DE DESIGN: verificam a ESTRUTURA (reflexão + NetArchTest). Começam vermelhos no código
/// em camadas horizontais e ficam verdes quando cada caso de uso vira uma fatia autocontida.
/// Usam só nomes (strings) para compilar nos dois estados do lab.
/// </summary>
public sealed class DesignVerticalSliceTests
{
    private const string NamespaceDasFeatures = "F4M05.Api.Features.Pedidos";
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static readonly string[] Commands = ["CriarPedido", "ConfirmarPedido", "CancelarPedido"];
    private static readonly string[] Queries = ["ListarPedidos", "ObterPedido"];
    private static readonly string[] Features = [.. Commands, .. Queries];

    private static Type Feature(string nome) =>
        Api.GetType($"{NamespaceDasFeatures}.{nome}")
        ?? throw new ShouldAssertException($"Fatia não encontrada: crie Features/Pedidos/{nome}.cs com 'public static class {nome}' no namespace {NamespaceDasFeatures}.");

    private static Type Aninhado(Type feature, string nome) =>
        feature.GetNestedType(nome, BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new ShouldAssertException($"{feature.Name} não tem o tipo aninhado '{nome}'. A fatia deve conter tudo o que o caso de uso precisa.");

    private static bool Implementa(Type tipo, string nomeDaInterfaceGenerica) =>
        tipo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == nomeDaInterfaceGenerica);

    // ---------- 1. Sem camadas horizontais ----------

    [Fact]
    public void Api_NaoTemPastasPorTipoTecnico()
    {
        Types.InAssembly(Api)
            .ShouldNot()
            .ResideInNamespaceMatching(@"^F4M05\.Api\.(Controllers|Services|Repositories|DTOs)(\..*)?$")
            .GetResult()
            .IsSuccessful.ShouldBeTrue("Ainda existem tipos em Controllers/Services/Repositories/DTOs: organize por feature.");
    }

    [Fact]
    public void Api_NaoTemServicesNemRepositoriosGenericos()
    {
        var genericos = Api.GetTypes()
            .Where(t => !t.IsNested)
            .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal)
                        || t.Name.EndsWith("Repository", StringComparison.Ordinal)
                        || t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        genericos.ShouldBeEmpty("Classes 'faz-tudo' por camada (PedidoService, PedidoRepository, PedidosController) devem sumir.");
    }

    // ---------- 2. Cada caso de uso é uma fatia autocontida ----------

    [Fact]
    public void CadaCasoDeUso_TemSuaPropriaFatia()
    {
        foreach (var nome in Features)
        {
            var feature = Feature(nome);
            (feature.IsAbstract && feature.IsSealed).ShouldBeTrue($"{nome} deve ser 'static class' (só agrupa os tipos da fatia).");
        }
    }

    [Fact]
    public void CadaFatia_TemHandlerEEndpointProprios()
    {
        foreach (var nome in Features)
        {
            var feature = Feature(nome);

            var handler = Aninhado(feature, "Handler");
            handler.IsSealed.ShouldBeTrue($"{nome}.Handler deve ser sealed.");

            var endpoint = Aninhado(feature, "Endpoint");
            endpoint.GetInterfaces().ShouldContain(i => i.Name == "IEndpoint", $"{nome}.Endpoint deve implementar IEndpoint.");
        }
    }

    [Fact]
    public void Commands_TemValidatorFluentValidationNaPropriaFatia()
    {
        foreach (var nome in Commands)
        {
            var feature = Feature(nome);
            var command = Aninhado(feature, "Command");
            var validator = Aninhado(feature, "Validator");

            validator.BaseType.ShouldNotBeNull();
            validator.BaseType!.Name.ShouldBe("AbstractValidator`1", $"{nome}.Validator deve herdar de AbstractValidator<Command>.");
            validator.BaseType.GetGenericArguments().ShouldBe([command]);
        }
    }

    // ---------- 3. CQRS natural ----------

    [Fact]
    public void Fatias_SeparamCommandsDeQueries()
    {
        foreach (var nome in Commands)
            Implementa(Aninhado(Feature(nome), "Handler"), "ICommandHandler`2").ShouldBeTrue($"{nome}.Handler deve implementar ICommandHandler<Command, Response>.");

        foreach (var nome in Queries)
        {
            var feature = Feature(nome);
            Aninhado(feature, "Query");
            Implementa(Aninhado(feature, "Handler"), "IQueryHandler`2").ShouldBeTrue($"{nome}.Handler deve implementar IQueryHandler<Query, ...>.");
        }
    }

    [Fact]
    public void PipelineDeValidacao_EnvolveOsCommandHandlers()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        foreach (var nome in Commands)
        {
            var handler = Aninhado(Feature(nome), "Handler");
            var contrato = handler.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "ICommandHandler`2");

            var resolvido = scope.ServiceProvider.GetRequiredService(contrato);

            resolvido.GetType().ShouldNotBe(handler,
                $"{nome}: quem resolve ICommandHandler<,> deve ser o decorator de validação, que então chama o Handler.");
            resolvido.GetType().Name.ShouldStartWith("Validacao");
        }
    }

    // ---------- 4. Fatias independentes, domínio compartilhado ----------

    [Fact]
    public void Fatias_NaoDependemUmasDasOutras()
    {
        foreach (var nome in Features)
        {
            Feature(nome); // a fatia precisa existir

            var usados = DependenciasDeFatia.TiposRaizUsadosPor(Api.Location, $"{NamespaceDasFeatures}.{nome}");
            var outrasFatias = Features
                .Where(f => f != nome)
                .Select(f => $"{NamespaceDasFeatures}.{f}")
                .Where(usados.Contains)
                .ToList();

            outrasFatias.ShouldBeEmpty(
                $"{nome} usa tipos de outra fatia. Compartilhe pelo domínio ou por Comum/, nunca acoplando uma fatia à outra.");
        }
    }

    [Fact]
    public void Dominio_ConcentraAsTransicoesDeStatus()
    {
        var pedido = Api.GetTypes().Single(t => t is { Name: "Pedido", IsClass: true, IsNested: false });

        pedido.GetMethod("Confirmar", Type.EmptyTypes).ShouldNotBeNull("A regra 'só confirma pedido Created' deve morar em Pedido.Confirmar(), uma vez só.");
        pedido.GetMethod("Cancelar", Type.EmptyTypes).ShouldNotBeNull("A regra de cancelamento deve morar em Pedido.Cancelar(), uma vez só.");
        pedido.GetProperty("Status")!.SetMethod?.IsPublic.ShouldNotBe(true, "Status com setter público permite pular as regras de transição.");
    }
}
