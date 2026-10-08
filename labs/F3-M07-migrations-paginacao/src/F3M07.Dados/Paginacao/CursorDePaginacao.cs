namespace F3M07.Dados.Paginacao;

/// <summary>Posição do último item entregue: a chave de ordenação (CriadoEm, Id).</summary>
public readonly record struct PosicaoCursor(DateTime CriadoEm, int Id);

/// <summary>Cursor recebido do cliente que não foi gerado por nós (ou foi adulterado).</summary>
public sealed class CursorInvalidoException(string mensagem, Exception? interna = null)
    : Exception(mensagem, interna);

/// <summary>
/// Cursor OPACO: o cliente só devolve o texto que recebeu; não monta, não interpreta.
/// Formato interno (13 bytes): versão (1) + CriadoEm.Ticks (8, big-endian) + Id (4, big-endian),
/// codificado em Base64Url (seguro em query string, sem '+', '/' nem '=').
/// </summary>
public static class CursorDePaginacao
{
    /// <summary>Passo 7: transforma a posição em texto opaco Base64Url.</summary>
    public static string Codificar(PosicaoCursor posicao) =>
        throw new NotImplementedException(
            "TODO Passo 7: monte 13 bytes (versão 1 + Ticks + Id, com BinaryPrimitives.Write*BigEndian) " +
            "e devolva System.Buffers.Text.Base64Url.EncodeToString(bytes).");

    /// <summary>
    /// Passo 7: volta do texto para a posição. Qualquer coisa que não seja um cursor válido
    /// (vazio, Base64 inválido, tamanho/versão errados, valores fora de faixa) lança
    /// <see cref="CursorInvalidoException"/> — que a API traduziria em 400.
    /// </summary>
    public static PosicaoCursor Decodificar(string cursor) =>
        throw new NotImplementedException(
            "TODO Passo 7: Base64Url.DecodeFromChars (FormatException → CursorInvalidoException); confira tamanho 13, " +
            "versão 1, Ticks dentro da faixa de DateTime e Id > 0.");
}
