using System.Globalization;

namespace F6M08.Consistencia.Consistencia;

/// <summary>
/// "Eu escrevi a versão <see cref="Versao"/> do pedido <see cref="PedidoId"/>; quero ler algo pelo menos tão novo."
/// <para>
/// Devolvido pela escrita (no status da operação assíncrona) e reenviado pelo cliente no header
/// <c>X-Consistency-Token</c>. Formato no fio: <c>{pedidoId sem hífens}.{versao}</c>,
/// ex.: <c>3f2504e04f8941d39a0c0305e82c3301.2</c>.
/// </para>
/// <para>
/// Em produção você provavelmente o tornaria opaco (base64) e talvez assinado (HMAC) para o cliente
/// não fabricar tokens — a ideia é a mesma do "session token" do Cosmos DB ou do LSN/GTID de réplicas.
/// </para>
/// </summary>
public readonly record struct TokenDeConsistencia(Guid PedidoId, long Versao)
{
    public override string ToString() => $"{PedidoId:N}.{Versao.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Lê o formato de <see cref="ToString"/>. Devolve <c>false</c> (sem lançar) para nulo, vazio,
    /// sem o ponto, GUID inválido, versão não numérica ou versão menor que 1.
    /// </summary>
    public static bool TryParse(string? texto, out TokenDeConsistencia token)
    {
        token = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        var partes = texto.Split('.');
        if (partes.Length != 2) return false;
        if (!Guid.TryParseExact(partes[0], "N", out var pedidoId)) return false;
        if (!long.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var versao) || versao < 1) return false;

        token = new TokenDeConsistencia(pedidoId, versao);
        return true;
    }
}
