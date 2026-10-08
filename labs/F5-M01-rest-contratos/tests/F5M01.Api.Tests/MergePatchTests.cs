using System.Text.Json.Nodes;
using F5M01.Api.Http;

namespace F5M01.Api.Tests;

/// <summary>Passo 4: JSON Merge Patch (RFC 7396). Os casos vêm do Apêndice A da RFC.</summary>
public sealed class MergePatchTests
{
    [Theory]
    [InlineData("""{"a":"b"}""", """{"a":"c"}""", """{"a":"c"}""")]
    [InlineData("""{"a":"b"}""", """{"b":"c"}""", """{"a":"b","b":"c"}""")]
    [InlineData("""{"a":"b"}""", """{"a":null}""", """{}""")]
    [InlineData("""{"a":"b","b":"c"}""", """{"a":null}""", """{"b":"c"}""")]
    [InlineData("""{"a":["b"]}""", """{"a":"c"}""", """{"a":"c"}""")]
    [InlineData("""{"a":"c"}""", """{"a":["b"]}""", """{"a":["b"]}""")]
    [InlineData("""{"a":{"b":"c"}}""", """{"a":{"b":"d","c":null}}""", """{"a":{"b":"d"}}""")]
    [InlineData("""{"a":[{"b":"c"}]}""", """{"a":[1]}""", """{"a":[1]}""")]
    [InlineData("""["a","b"]""", """["c","d"]""", """["c","d"]""")]
    [InlineData("""{"a":"b"}""", """["c"]""", """["c"]""")]
    [InlineData("""{"a":"foo"}""", "null", "null")]
    [InlineData("""{"a":"foo"}""", "\"bar\"", "\"bar\"")]
    [InlineData("""{"e":null}""", """{"a":1}""", """{"e":null,"a":1}""")]
    [InlineData("""[1,2]""", """{"a":"b","c":null}""", """{"a":"b"}""")]
    [InlineData("""{}""", """{"a":{"bb":{"ccc":null}}}""", """{"a":{"bb":{}}}""")]
    public void Aplicar_ExemplosDaRfc7396(string alvo, string patch, string esperado)
    {
        var resultado = MergePatch.Aplicar(JsonNode.Parse(alvo), JsonNode.Parse(patch));

        JsonNode.DeepEquals(resultado, JsonNode.Parse(esperado)).ShouldBeTrue(
            $"esperado {esperado}, veio {resultado?.ToJsonString() ?? "null"}");
    }

    [Fact]
    public void Aplicar_NaoAlteraOAlvoNemOPatch()
    {
        var alvo = JsonNode.Parse("""{"observacao":"x","enderecoEntrega":{"cidade":"SP","cep":"01000-000"}}""");
        var patch = JsonNode.Parse("""{"observacao":null,"enderecoEntrega":{"cep":"02000-000"}}""");
        var alvoAntes = alvo!.ToJsonString();
        var patchAntes = patch!.ToJsonString();

        var resultado = MergePatch.Aplicar(alvo, patch);

        alvo.ToJsonString().ShouldBe(alvoAntes);
        patch.ToJsonString().ShouldBe(patchAntes);
        JsonNode.DeepEquals(resultado, JsonNode.Parse("""{"enderecoEntrega":{"cidade":"SP","cep":"02000-000"}}""")).ShouldBeTrue();
    }
}
