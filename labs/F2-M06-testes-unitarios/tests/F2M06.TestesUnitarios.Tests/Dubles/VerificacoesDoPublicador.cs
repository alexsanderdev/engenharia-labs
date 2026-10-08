using F2M06.TestesUnitarios.Aplicacao;

namespace F2M06.TestesUnitarios.Tests.Dubles;

/// <summary>
/// Asserts customizados para o MOCK do publicador. Publicar o evento é efeito colateral observável
/// por outros sistemas (dependência não gerenciada) — é exatamente onde verificar interação faz sentido.
/// </summary>
public static class VerificacoesDoPublicador
{
    /// <summary>
    /// Verifica que <paramref name="esperado"/> foi publicado exatamente UMA vez.
    /// <see cref="PedidoCriado"/> é um record: igualdade por valor deixa o match exato e legível.
    /// </summary>
    public static void DeveTerPublicadoUmaVez(this IPublicadorDeEventos publicador, PedidoCriado esperado) =>
        throw new NotImplementedException("TODO (Passo 6): publicador.Received(1).PublicarAsync(esperado, Arg.Any<CancellationToken>()).");

    /// <summary>Verifica que nenhum evento foi publicado (com quaisquer argumentos).</summary>
    public static void NaoDeveTerPublicadoNada(this IPublicadorDeEventos publicador) =>
        throw new NotImplementedException("TODO (Passo 6): publicador.DidNotReceiveWithAnyArgs().PublicarAsync(default!, default).");
}
