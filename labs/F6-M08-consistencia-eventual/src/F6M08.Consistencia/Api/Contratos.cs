namespace F6M08.Consistencia.Api;

/// <summary>Corpo do <c>POST /pedidos</c>.</summary>
public sealed record CriarPedidoRequest(Guid ClienteId, decimal Total);

/// <summary>
/// Corpo do 202 e do <c>GET /operacoes/{id}</c>.
/// <para><see cref="Status"/> vai como texto (<c>"Processando"</c>, <c>"Concluida"</c>, <c>"Falhou"</c>).</para>
/// <para>Quando concluída: <see cref="PedidoId"/>, <see cref="Versao"/>, <see cref="TokenDeConsistencia"/>
/// (o texto de <c>TokenDeConsistencia.ToString()</c>) e <see cref="Recurso"/> (<c>/clientes/{clienteId}/resumo</c>).</para>
/// </summary>
public sealed record RespostaDaOperacao(
    Guid OperacaoId,
    string Status,
    Guid? PedidoId = null,
    long? Versao = null,
    string? TokenDeConsistencia = null,
    string? Recurso = null,
    string? Erro = null);

/// <summary>Nomes de headers usados pela API. Pronto.</summary>
public static class Cabecalhos
{
    /// <summary>Requisição: "quero ler pelo menos esta versão" (read-your-writes).</summary>
    public const string TokenDeConsistencia = "X-Consistency-Token";

    /// <summary>Resposta do resumo: <c>projecao</c> ou <c>fonte</c>.</summary>
    public const string OrigemDaLeitura = "X-Read-Source";
}
