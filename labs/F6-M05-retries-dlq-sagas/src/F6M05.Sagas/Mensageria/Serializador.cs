using System.Text.Json;

namespace F6M05.Sagas.Mensageria;

/// <summary>PRONTO. JSON das mensagens (camelCase, polimorfismo via <c>$tipo</c> nas hierarquias da saga).</summary>
public static class Serializador
{
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    public static byte[] Serializar<T>(T valor) => JsonSerializer.SerializeToUtf8Bytes(valor, Opcoes);

    /// <summary>Lança <see cref="JsonException"/> se o JSON for inválido ou vier <c>null</c>.</summary>
    public static T Desserializar<T>(ReadOnlyMemory<byte> corpo) =>
        JsonSerializer.Deserialize<T>(corpo.Span, Opcoes)
        ?? throw new JsonException($"Corpo nulo para {typeof(T).Name}.");
}
