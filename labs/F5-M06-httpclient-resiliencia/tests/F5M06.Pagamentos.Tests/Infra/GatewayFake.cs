using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace F5M06.Pagamentos.Tests.Infra;

/// <summary>
/// "Gateway de pagamento" simulado com WireMock.Net: um servidor HTTP de verdade em porta aleatória.
/// O cliente passa por toda a pilha real (HttpClient, handlers, sockets), e o log de requisições do
/// WireMock é a prova de quantas tentativas saíram e com quais headers.
/// (Infra pronta: você não precisa alterar este arquivo.)
/// </summary>
public sealed class GatewayFake : IDisposable
{
    public const string RotaCobrancas = "/v1/cobrancas";

    public WireMockServer Servidor { get; } = WireMockServer.Start();

    /// <summary>Chave obviamente de teste, gerada por execução.</summary>
    public string ApiKey { get; } = "chave-de-teste-" + Guid.NewGuid().ToString("N");

    public Uri Url => new(Servidor.Url! + "/");

    public static string RotaStatus(string transacaoId) => $"{RotaCobrancas}/{transacaoId}";

    public static string RotaEstorno(string transacaoId) => $"{RotaCobrancas}/{transacaoId}/estornos";

    public static IRequestBuilder PostCobranca() => Request.Create().WithPath(RotaCobrancas).UsingPost();

    public static IRequestBuilder GetStatus(string transacaoId) => Request.Create().WithPath(RotaStatus(transacaoId)).UsingGet();

    public static IRequestBuilder PostEstorno(string transacaoId) => Request.Create().WithPath(RotaEstorno(transacaoId)).UsingPost();

    public static IResponseBuilder Status(int status) => Response.Create().WithStatusCode(status);

    public static IResponseBuilder Aprovada(string transacaoId = "tx_123") =>
        Response.Create().WithStatusCode(201).WithBodyAsJson(new { transacaoId, status = "aprovada" });

    public static IResponseBuilder StatusDaTransacao(string transacaoId, string status) =>
        Response.Create().WithStatusCode(200).WithBodyAsJson(new { transacaoId, status });

    public static IResponseBuilder Recusada(string codigo) =>
        Response.Create().WithStatusCode(402).WithBodyAsJson(new { codigo, mensagem = "Cartão recusado pelo emissor" });

    /// <summary>Responde sempre o mesmo, para qualquer requisição que case.</summary>
    public void Sempre(IRequestBuilder requisicao, IResponseBuilder resposta) =>
        Servidor.Given(requisicao).RespondWith(resposta);

    /// <summary>
    /// Responde em sequência (cenário/state machine do WireMock): 1ª requisição recebe a 1ª resposta,
    /// 2ª recebe a 2ª, ... e a última se repete daí em diante.
    /// </summary>
    public void EmSequencia(IRequestBuilder requisicao, params IResponseBuilder[] respostas)
    {
        var cenario = "seq-" + Guid.NewGuid().ToString("N");
        for (var i = 0; i < respostas.Length; i++)
        {
            // O mapping inicial (sem WhenStateIs) casa em qualquer estado: a prioridade
            // (número menor = mais forte) garante que o passo mais avançado vença.
            var mapping = Servidor.Given(requisicao).AtPriority(1_000 - i).InScenario(cenario);
            if (i > 0)
            {
                mapping = mapping.WhenStateIs($"passo-{i}");
            }

            // o último passo "se reagenda" para continuar valendo nas próximas requisições
            mapping = mapping.WillSetStateTo($"passo-{Math.Min(i + 1, respostas.Length - 1)}");

            mapping.RespondWith(respostas[i]);
        }
    }

    /// <summary>Remove todos os mappings (o log continua).</summary>
    public void LimparRespostas() => Servidor.ResetMappings();

    /// <summary>
    /// Requisições recebidas numa rota. Espera (no máximo 2 s) até haver pelo menos
    /// <paramref name="minimoEsperado"/>, porque o WireMock grava o log logo DEPOIS de responder.
    /// </summary>
    public async Task<IReadOnlyList<IRequestMessage>> RequisicoesAsync(string rota, int minimoEsperado = 0)
    {
        var limite = DateTime.UtcNow.AddSeconds(2);
        while (true)
        {
            var recebidas = Servidor.LogEntries
                .Select(e => e.RequestMessage)
                .OfType<IRequestMessage>()
                .Where(r => r.Path == rota)
                .OrderBy(r => r.DateTime)
                .ToList();

            if (recebidas.Count >= minimoEsperado || DateTime.UtcNow > limite)
            {
                return recebidas;
            }

            await Task.Delay(5);
        }
    }

    public static string? Header(IRequestMessage requisicao, string nome) =>
        requisicao.Headers is { } headers && headers.TryGetValue(nome, out var valores) ? valores.FirstOrDefault() : null;

    public void Dispose()
    {
        Servidor.Stop();
        Servidor.Dispose();
    }
}
