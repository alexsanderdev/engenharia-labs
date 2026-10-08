using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace F1M06.Hosting;

/// <summary>Monta o Generic Host do worker do OrderFlow.</summary>
public static class OrderFlowHost
{
    /// <summary>
    /// Cria (sem iniciar) o host:
    /// <list type="number">
    /// <item><c>Host.CreateApplicationBuilder</c> com <c>DisableDefaults = true</c> (testes determinísticos: sem
    /// appsettings, variáveis de ambiente nem console logger implícitos).</item>
    /// <item>Acrescenta <paramref name="configuracao"/> em memória (se houver).</item>
    /// <item>Chama <see cref="ComposicaoOrderFlow.AddOrderFlow"/> e, DEPOIS, <paramref name="configurarServicos"/>
    /// (o teste sobrescreve registros: o último registro vence).</item>
    /// <item>Liga <c>ValidateOnBuild</c> e <c>ValidateScopes</c> SEMPRE (não só em Development) via
    /// <c>ConfigureContainer(new DefaultServiceProviderFactory(...))</c>.</item>
    /// </list>
    /// </summary>
    public static IHost Criar(
        IDictionary<string, string?>? configuracao = null,
        Action<IServiceCollection>? configurarServicos = null)
    {
        throw new NotImplementedException("TODO: siga os 4 passos do comentário acima e devolva builder.Build()");
    }
}
