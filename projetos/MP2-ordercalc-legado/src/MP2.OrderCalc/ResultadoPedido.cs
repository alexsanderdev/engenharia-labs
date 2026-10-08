#nullable disable
// Contrato legado (outros sistemas leem estes campos públicos): exceção documentada às regras CA1051/CA2211.
#pragma warning disable CA1051, CA2211
namespace MP2.OrderCalc;

// DTO de saída consumido pela tela antiga e por um relatório em VB6 (sim).
// O formato é contrato: não renomeie nem remova campos.
public class ResultadoPedido
{
    public string Numero;
    public string Status; // "OK", "ERRO", "ORCAMENTO"
    public string Erro;
    public decimal Subtotal;
    public decimal Desconto;
    public decimal Frete;
    public decimal Imposto;
    public decimal Total;
    public DateTime PrazoEntrega;
    public DateTime CriadoEm;
    public List<string> Linhas = new List<string>();
    public List<string> Mensagens = new List<string>();
}
