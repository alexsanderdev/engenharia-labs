using System.Reflection;
using F4M01.Application.Abstracoes;
using F4M01.Domain.Comum;
using F4M01.Infrastructure.Persistencia;

namespace F4M01.Arquitetura.Tests;

/// <summary>
/// Âncoras estáveis de cada camada (tipos que NÃO mudam durante o lab, para os testes compilarem
/// no código inicial e na solução).
/// </summary>
internal static class Camadas
{
    public const string Domain = "F4M01.Domain";
    public const string Application = "F4M01.Application";
    public const string Infrastructure = "F4M01.Infrastructure";
    public const string Api = "F4M01.Api";
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string AspNetCore = "Microsoft.AspNetCore";

    public static readonly Assembly DomainAssembly = typeof(DomainException).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(IRelogio).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(OrderFlowDbContext).Assembly;
    public static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    /// <summary>Falha listando os tipos culpados (sem isso, regra quebrada vira "expected True").</summary>
    public static void DeveSerRespeitada(this NetArchTest.Rules.TestResult resultado, string regra)
    {
        var culpados = resultado.FailingTypeNames ?? [];
        resultado.IsSuccessful.ShouldBeTrue(
            $"Regra violada: {regra}{Environment.NewLine}Tipos: {string.Join(", ", culpados)}");
    }
}
