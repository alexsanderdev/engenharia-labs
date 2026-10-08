using System.Buffers.Binary;
using System.Buffers.Text;

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
    private const byte VersaoDoFormato = 1;
    private const int Tamanho = 1 + sizeof(long) + sizeof(int);

    /// <summary>Passo 7: transforma a posição em texto opaco Base64Url.</summary>
    public static string Codificar(PosicaoCursor posicao)
    {
        Span<byte> bytes = stackalloc byte[Tamanho];
        bytes[0] = VersaoDoFormato;
        BinaryPrimitives.WriteInt64BigEndian(bytes[1..], posicao.CriadoEm.Ticks);
        BinaryPrimitives.WriteInt32BigEndian(bytes[9..], posicao.Id);
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>
    /// Passo 7: volta do texto para a posição. Qualquer coisa que não seja um cursor válido
    /// (vazio, Base64 inválido, tamanho/versão errados, valores fora de faixa) lança
    /// <see cref="CursorInvalidoException"/> — que a API traduziria em 400.
    /// </summary>
    public static PosicaoCursor Decodificar(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            throw new CursorInvalidoException("Cursor vazio.");

        byte[] bytes;
        try
        {
            bytes = Base64Url.DecodeFromChars(cursor);
        }
        catch (FormatException ex)
        {
            throw new CursorInvalidoException("Cursor não é Base64Url válido.", ex);
        }

        if (bytes.Length != Tamanho || bytes[0] != VersaoDoFormato)
            throw new CursorInvalidoException("Cursor com formato desconhecido.");

        var ticks = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(1));
        var id = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(9));
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks || id <= 0)
            throw new CursorInvalidoException("Cursor com valores fora da faixa.");

        return new PosicaoCursor(new DateTime(ticks), id);
    }
}
