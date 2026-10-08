using Microsoft.Extensions.DependencyInjection;

namespace F1M06.Hosting;

/// <summary>Valida uma composição antes de ela chegar em produção.</summary>
public static class ValidacaoDoContainer
{
    /// <summary>
    /// Constrói um provider com <c>ValidateOnBuild</c> e <c>ValidateScopes</c> ligados e devolve as mensagens
    /// de erro encontradas (lista vazia = composição válida). A validação lança <see cref="AggregateException"/>
    /// com uma exceção interna por registro problemático.
    /// </summary>
    public static IReadOnlyList<string> EncontrarProblemas(IServiceCollection services)
    {
        throw new NotImplementedException("TODO: BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }); capture AggregateException e devolva as mensagens das InnerExceptions");
    }
}
