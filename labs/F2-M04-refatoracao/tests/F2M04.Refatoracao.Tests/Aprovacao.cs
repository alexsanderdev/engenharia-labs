using System.Runtime.CompilerServices;
using System.Text;

namespace F2M04.Refatoracao.Tests;

/// <summary>
/// Approval test de bolso, com as MESMAS convenções do Verify (https://github.com/VerifyTests/Verify):
/// compara o texto com <c>&lt;Classe&gt;.&lt;Metodo&gt;.verified.txt</c>, ao lado do arquivo de teste.
/// Se for diferente (ou o arquivo não existir), grava <c>&lt;Classe&gt;.&lt;Metodo&gt;.received.txt</c> e falha.
/// Para aprovar uma mudança INTENCIONAL: revise o .received.txt e renomeie para .verified.txt.
/// O .gitignore do repo ignora <c>*.received.*</c>; o .verified.txt é versionado.
/// </summary>
/// <remarks>
/// Por que não o pacote Verify.XunitV3 direto? Desde a série 33, o build do Verify exige uma propriedade
/// de licença/patrocínio (SponsorCheck SC021). Veja o README do lab para trocar este helper pelo Verify.
/// </remarks>
internal static class Aprovacao
{
    public static void Verificar(string texto, [CallerFilePath] string arquivoDoTeste = "", [CallerMemberName] string metodo = "")
    {
        var pasta = Path.GetDirectoryName(arquivoDoTeste)!;
        var classe = Path.GetFileNameWithoutExtension(arquivoDoTeste);
        var verificado = Path.Combine(pasta, $"{classe}.{metodo}.verified.txt");
        var recebido = Path.Combine(pasta, $"{classe}.{metodo}.received.txt");

        var atual = Normalizar(texto);
        if (File.Exists(verificado))
        {
            var aprovado = Normalizar(File.ReadAllText(verificado));
            if (aprovado == atual)
            {
                if (File.Exists(recebido)) File.Delete(recebido);
                return;
            }

            File.WriteAllText(recebido, atual, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Assert.Fail($"""
                O resultado mudou em relação ao aprovado. {PrimeiraDiferenca(aprovado, atual)}
                Compare os arquivos:
                  aprovado: {verificado}
                  recebido: {recebido}
                Se a mudança NÃO era intencional, desfaça o último passo da refatoração.
                Se era (ex.: regra nova pedida pelo negócio), renomeie o .received.txt para .verified.txt e faça commit.
                """);
        }

        File.WriteAllText(recebido, atual, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Assert.Fail($"Ainda não há resultado aprovado. Revise e renomeie para .verified.txt:\n  {recebido}");
    }

    private static string Normalizar(string texto) => texto.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string PrimeiraDiferenca(string aprovado, string atual)
    {
        var linhasAprovadas = aprovado.Split('\n');
        var linhasAtuais = atual.Split('\n');
        for (var i = 0; i < Math.Max(linhasAprovadas.Length, linhasAtuais.Length); i++)
        {
            var esperado = i < linhasAprovadas.Length ? linhasAprovadas[i] : "(fim do arquivo)";
            var obtido = i < linhasAtuais.Length ? linhasAtuais[i] : "(fim do arquivo)";
            if (esperado != obtido)
                return $"Primeira diferença na linha {i + 1}:\n  aprovado: {esperado}\n  recebido: {obtido}";
        }

        return string.Empty;
    }
}
