using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MP2.OrderCalc.Tests;

/// <summary>
/// Approval test mínimo (golden master): serializa o valor em JSON e compara com
/// <c>&lt;Classe&gt;.&lt;Metodo&gt;.approved.txt</c>, ao lado do arquivo de teste.
/// Se for diferente (ou não existir), grava <c>.received.txt</c> e falha. Para aprovar,
/// revise o .received.txt e renomeie para .approved.txt.
/// É o que o Verify faz, em versão de bolso. Ver README (seção Verify).
/// </summary>
public static class Aprovacao
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Verificar(object valor, [CallerFilePath] string arquivo = "", [CallerMemberName] string metodo = "")
    {
        var pasta = Path.GetDirectoryName(arquivo)!;
        var classe = Path.GetFileNameWithoutExtension(arquivo);
        var aprovado = Path.Combine(pasta, $"{classe}.{metodo}.approved.txt");
        var recebido = Path.Combine(pasta, $"{classe}.{metodo}.received.txt");

        var atual = Normalizar(JsonSerializer.Serialize(valor, Opcoes));
        if (File.Exists(aprovado) && Normalizar(File.ReadAllText(aprovado)) == atual)
        {
            if (File.Exists(recebido)) File.Delete(recebido);
            return;
        }

        File.WriteAllText(recebido, atual);
        Assert.Fail(File.Exists(aprovado)
            ? $"Resultado diferente do aprovado. Compare:\n  {aprovado}\n  {recebido}"
            : $"Ainda não há resultado aprovado. Revise e renomeie para .approved.txt:\n  {recebido}");
    }

    private static string Normalizar(string texto) => texto.Replace("\r\n", "\n").TrimEnd() + "\n";
}
