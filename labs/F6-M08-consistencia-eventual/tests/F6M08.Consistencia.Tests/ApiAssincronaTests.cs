using System.Net;
using System.Net.Http.Json;
using F6M08.Consistencia.Api;
using F6M08.Consistencia.Tests.Infra;
using static F6M08.Consistencia.Tests.Infra.Eventos;

namespace F6M08.Consistencia.Tests;

/// <summary>
/// Passo 5 — a borda: 202 Accepted + Location de status, e read-your-writes pelo header X-Consistency-Token.
/// </summary>
public sealed class ApiAssincronaTests
{
    [Fact]
    public async Task CriarPedido_Retorna202ComLocationDeStatusERetryAfter()
    {
        await using var api = new ApiFactory();
        using var cliente = api.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/pedidos", new { clienteId = Ana, total = 100m }, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        resposta.Headers.Location!.OriginalString.ShouldStartWith("/operacoes/");
        resposta.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(1));
        var corpo = await resposta.JsonAsync();
        corpo.GetProperty("status").GetString().ShouldBe("Processando");
        resposta.Headers.Location.OriginalString.ShouldEndWith(corpo.GetProperty("operacaoId").GetString()!);

        (await cliente.GetAsync($"/operacoes/{Guid.NewGuid()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await cliente.PostAsJsonAsync("/pedidos", new { clienteId = Ana, total = 0m }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Operacao_FicaProcessandoAteOComandoRodar_DepoisTrazTokenDeConsistencia()
    {
        await using var api = new ApiFactory();
        using var cliente = api.CreateClient();
        var location = (await cliente.PostAsJsonAsync("/pedidos", new { clienteId = Ana, total = 100m }, Ct)).Headers.Location!;

        var status = await cliente.GetAsync(location, Ct);
        status.StatusCode.ShouldBe(HttpStatusCode.OK);
        status.Headers.RetryAfter.ShouldNotBeNull();
        (await status.JsonAsync()).GetProperty("status").GetString().ShouldBe("Processando", "o relógio não andou: o comando está na fila");

        api.Relogio.Advance(TimeSpan.FromSeconds(2));
        var concluida = await cliente.AguardarOperacaoAsync(location);

        concluida.GetProperty("status").GetString().ShouldBe("Concluida");
        var pedidoId = concluida.GetProperty("pedidoId").GetGuid();
        concluida.GetProperty("versao").GetInt64().ShouldBe(1);
        concluida.GetProperty("tokenDeConsistencia").GetString().ShouldBe($"{pedidoId:N}.1");
        concluida.GetProperty("recurso").GetString().ShouldBe($"/clientes/{Ana}/resumo");
    }

    [Fact]
    public async Task Resumo_ComToken_LeAPropriaEscritaMesmoComAProjecaoAtrasada()
    {
        await using var api = new ApiFactory();
        using var cliente = api.CreateClient();
        var location = (await cliente.PostAsJsonAsync("/pedidos", new { clienteId = Ana, total = 100m }, Ct)).Headers.Location!;
        api.Relogio.Advance(TimeSpan.FromSeconds(2));
        var token = (await cliente.AguardarOperacaoAsync(location)).GetProperty("tokenDeConsistencia").GetString()!;

        // Sem token: a projeção ainda não recebeu o evento (atraso de 3 s não passou). "Cadê meu pedido?"
        var semToken = await cliente.GetAsync($"/clientes/{Ana}/resumo", Ct);
        semToken.Headers.GetValues(Cabecalhos.OrigemDaLeitura).ShouldBe(["projecao"]);
        (await semToken.JsonAsync()).GetProperty("quantidade").GetInt32().ShouldBe(0);

        // Com token: a requisição espera a projeção alcançar a versão escrita.
        using var comToken = new HttpRequestMessage(HttpMethod.Get, $"/clientes/{Ana}/resumo");
        comToken.Headers.Add(Cabecalhos.TokenDeConsistencia, token);
        var pendente = cliente.SendAsync(comToken, Ct);
        api.Relogio.Advance(TimeSpan.FromSeconds(3));
        var resposta = await pendente.WaitAsync(LimiteDeSeguranca, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        resposta.Headers.GetValues(Cabecalhos.OrigemDaLeitura).ShouldBe(["projecao"]);
        var resumo = await resposta.JsonAsync();
        resumo.GetProperty("quantidade").GetInt32().ShouldBe(1);
        resumo.GetProperty("valorEmAberto").GetDecimal().ShouldBe(100m);
    }

    [Fact]
    public async Task Resumo_ComTokenSemEspera_CaiParaAFonte_ETokenInvalidoDa400()
    {
        await using var api = new ApiFactory(o => o.EsperaMaximaDaLeitura = TimeSpan.Zero);
        using var cliente = api.CreateClient();
        var location = (await cliente.PostAsJsonAsync("/pedidos", new { clienteId = Ana, total = 42m }, Ct)).Headers.Location!;
        api.Relogio.Advance(TimeSpan.FromSeconds(2));
        var token = (await cliente.AguardarOperacaoAsync(location)).GetProperty("tokenDeConsistencia").GetString()!;

        using var comToken = new HttpRequestMessage(HttpMethod.Get, $"/clientes/{Ana}/resumo");
        comToken.Headers.Add(Cabecalhos.TokenDeConsistencia, token);
        var resposta = await cliente.SendAsync(comToken, Ct);

        resposta.Headers.GetValues(Cabecalhos.OrigemDaLeitura).ShouldBe(["fonte"]);
        (await resposta.JsonAsync()).GetProperty("valorEmAberto").GetDecimal().ShouldBe(42m);

        using var invalido = new HttpRequestMessage(HttpMethod.Get, $"/clientes/{Ana}/resumo");
        invalido.Headers.Add(Cabecalhos.TokenDeConsistencia, "isso-nao-e-token");
        var erro = await cliente.SendAsync(invalido, Ct);
        erro.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        erro.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }
}
