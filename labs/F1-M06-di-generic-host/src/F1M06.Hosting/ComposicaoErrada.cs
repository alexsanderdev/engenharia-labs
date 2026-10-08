using Microsoft.Extensions.DependencyInjection;

namespace F1M06.Hosting;

/// <summary>
/// EXEMPLO DO QUE NÃO FAZER (não altere): um Singleton que depende de um serviço Scoped.
/// O singleton vive para sempre e "captura" o primeiro <see cref="IContextoDaOperacao"/> que receber
/// (captive dependency): todas as requisições passam a compartilhar o contexto da primeira.
/// </summary>
public sealed class CacheDeCatalogo(IContextoDaOperacao contexto)
{
    public Guid ContextoCapturado => contexto.Id;
}

/// <summary>Registros propositalmente errados, usados pelos testes de validação.</summary>
public static class ComposicaoErrada
{
    public static IServiceCollection AddComDependenciaCativa(this IServiceCollection services)
    {
        services.AddScoped<IContextoDaOperacao, ContextoDaOperacao>();
        services.AddSingleton<CacheDeCatalogo>(); // ← singleton consumindo scoped
        return services;
    }
}
