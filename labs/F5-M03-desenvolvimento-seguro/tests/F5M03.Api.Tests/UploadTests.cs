using System.Net;
using System.Net.Http.Headers;
using System.Text;
using F5M03.Api.Dominio;
using F5M03.Api.Produtos;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 8 — entrada sem limite (OWASP API4:2023, Unrestricted Resource Consumption).
/// Upload sem teto de tamanho derruba o servidor por memória; sem allowlist de tipo vira
/// hospedagem de HTML/executável com a "cara" do seu domínio.
/// </summary>
public sealed class UploadTests : TesteDeApi
{
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private Task<HttpResponseMessage> EnviarAsync(byte[] bytes, string contentType)
    {
        var conteudo = new ByteArrayContent(bytes);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return Client.PostAsync($"/produtos/{ProdutosConhecidos.TecladoId}/imagem", conteudo, Ct);
    }

    private byte[]? ImagemGuardada() => Api.Produtos.Obter(ProdutosConhecidos.TecladoId)!.Imagem;

    [Fact]
    public async Task Upload_AcimaDoLimite_Retorna413ENaoGuarda()
    {
        var grande = new byte[LimitesDeUpload.TamanhoMaximoEmBytes + 1];
        AssinaturaPng.CopyTo(grande, 0);

        var resposta = await EnviarAsync(grande, "image/png");

        resposta.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        ImagemGuardada().ShouldBeNull();
    }

    [Fact]
    public async Task Upload_ExatamenteNoLimite_Aceita()
    {
        var noLimite = new byte[LimitesDeUpload.TamanhoMaximoEmBytes];
        AssinaturaPng.CopyTo(noLimite, 0);

        var resposta = await EnviarAsync(noLimite, "image/png");

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Upload_TipoForaDaAllowlist_Retorna415()
    {
        var resposta = await EnviarAsync(Encoding.UTF8.GetBytes("<script>alert(document.cookie)</script>"), "text/html");

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        ImagemGuardada().ShouldBeNull();
    }

    [Fact]
    public async Task Upload_DeclaraPngMasOsBytesSaoExecutavel_Retorna415()
    {
        byte[] executavel = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]; // "MZ": cabeçalho de .exe

        var resposta = await EnviarAsync(executavel, "image/png");

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        ImagemGuardada().ShouldBeNull();
    }
}
