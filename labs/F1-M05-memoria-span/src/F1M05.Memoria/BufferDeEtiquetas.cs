using System.Buffers;

namespace F1M05.Memoria;

/// <summary>
/// Acumula códigos formatados (um por linha, terminados em '\n') em um buffer alugado de um
/// <see cref="ArrayPool{T}"/>. Como segura um recurso emprestado, implementa <see cref="IDisposable"/>:
/// quem cria usa <c>using</c> e o buffer volta ao pool no <see cref="Dispose"/>.
/// </summary>
public sealed class BufferDeEtiquetas : IDisposable
{
    // Sugestão de estado: o pool, o array alugado (null depois do Dispose) e a posição de escrita.
    // private readonly ArrayPool<char> _pool;
    // private char[]? _buffer;
    // private int _posicao;

    /// <summary>Aluga o buffer inicial do pool (padrão: <see cref="ArrayPool{T}.Shared"/>).</summary>
    public BufferDeEtiquetas(int capacidadeInicial = 64, ArrayPool<char>? pool = null)
    {
        throw new NotImplementedException("TODO: valide a capacidade, guarde o pool e alugue o buffer inicial");
    }

    /// <summary>Quantidade de códigos adicionados.</summary>
    public int Quantidade { get; private set; }

    /// <summary>
    /// Conteúdo acumulado até agora, sem copiar. Não guarde esse span depois do Dispose:
    /// o array volta ao pool e pode ser reutilizado por outro código.
    /// </summary>
    public ReadOnlySpan<char> Conteudo =>
        throw new NotImplementedException("TODO: devolva a parte preenchida do buffer (ObjectDisposedException se já liberado)");

    /// <summary>
    /// Escreve o código + '\n'. Se não couber, aluga um array com o dobro do tamanho (ou o necessário),
    /// copia o conteúdo e DEVOLVE o antigo ao pool.
    /// Lança <see cref="ObjectDisposedException"/> se usado depois do Dispose.
    /// </summary>
    public void Adicionar(CodigoPedido codigo)
    {
        throw new NotImplementedException("TODO: garanta espaço (crescendo via pool), formate com ParserCodigoPedido.TryFormat e acrescente '\\n'");
    }

    /// <summary>
    /// Devolve o buffer ao pool. Idempotente: chamar duas vezes devolve uma vez só
    /// (devolver o mesmo array duas vezes corromperia o pool).
    /// </summary>
    public void Dispose()
    {
        throw new NotImplementedException("TODO: devolva o buffer ao pool uma única vez e marque como liberado");
    }
}
