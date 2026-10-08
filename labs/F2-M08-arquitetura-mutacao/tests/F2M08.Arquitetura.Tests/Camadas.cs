using System.Reflection;
using F2M08.Application.Abstracoes;
using F2M08.Domain.Entidades;
using F2M08.Infrastructure.Persistencia;
using NetArchTest.Rules;

namespace F2M08.Arquitetura.Tests;

/// <summary>
/// Pontos de entrada estáveis para cada camada. Usamos tipos que NÃO mudam durante o lab
/// (assim renomear/mover classes não quebra a compilação dos testes).
/// </summary>
internal static class Camadas
{
    public const string Domain = "F2M08.Domain";
    public const string Application = "F2M08.Application";
    public const string Infrastructure = "F2M08.Infrastructure";
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";

    public static readonly Assembly DomainAssembly = typeof(Pedido).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(ICommandHandler<,>).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(OrderFlowDbContext).Assembly;

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
