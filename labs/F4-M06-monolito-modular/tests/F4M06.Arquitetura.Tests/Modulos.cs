using System.Reflection;
using F4M06.Catalogo;
using F4M06.Catalogo.Contracts;
using F4M06.Clientes;
using F4M06.Clientes.Contracts;
using F4M06.Pedidos;
using F4M06.Pedidos.Contracts;
using F4M06.Shared.Modulos;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Arquitetura.Tests;

/// <summary>Um módulo = projeto de implementação + projeto de contratos.</summary>
internal sealed record Modulo(string Nome, Assembly Implementacao, Assembly Contratos)
{
    /// <summary>Nomes completos dos tipos de topo da IMPLEMENTAÇÃO (públicos e internos).</summary>
    public string[] TiposDaImplementacao() =>
        [.. Implementacao.GetTypes()
            .Where(t => !t.IsNested && !t.Name.StartsWith('<'))
            .Select(t => t.FullName!)];

    /// <summary>Tipos da implementação que herdam de <see cref="DbContext"/>.</summary>
    public Type[] DbContexts() =>
        [.. Implementacao.GetTypes().Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)];
}

/// <summary>
/// Pontos de entrada estáveis de cada módulo. Usamos tipos que NÃO mudam durante o lab
/// (a classe do módulo e um tipo do contrato), então mover/renomear classes internas não quebra os testes.
/// </summary>
internal static class Modulos
{
    public static readonly Modulo Catalogo = new("Catalogo", typeof(CatalogoModule).Assembly, typeof(ICatalogoApi).Assembly);
    public static readonly Modulo Pedidos = new("Pedidos", typeof(PedidosModule).Assembly, typeof(PedidoConfirmado).Assembly);
    public static readonly Modulo Clientes = new("Clientes", typeof(ClientesModule).Assembly, typeof(IClientesApi).Assembly);

    public static readonly Modulo[] Todos = [Catalogo, Pedidos, Clientes];

    public static readonly Assembly Shared = typeof(IModule).Assembly;

    public static Modulo PorNome(string nome) => Todos.Single(m => m.Nome == nome);

    public static IEnumerable<Modulo> OutrosAlemDe(string nome) => Todos.Where(m => m.Nome != nome);

    /// <summary>
    /// Falha com uma mensagem que LISTA os tipos culpados. Sem isso, regra de arquitetura
    /// quebrada vira "Expected True but was False" e ninguém sabe o que corrigir.
    /// </summary>
    public static void DeveSerRespeitada(this NetArchTest.Rules.TestResult resultado, string regra)
    {
        var culpados = resultado.FailingTypeNames ?? [];
        resultado.IsSuccessful.ShouldBeTrue(
            $"Regra violada: {regra}{Environment.NewLine}Tipos: {string.Join(", ", culpados)}");
    }
}
