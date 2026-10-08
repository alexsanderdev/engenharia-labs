using System.Buffers;

namespace F1M05.Memoria;

/// <summary>
/// Acumula códigos formatados (um por linha, terminados em '\n') em um buffer alugado de um
/// <see cref="ArrayPool{T}"/>. Como segura um recurso emprestado, implementa <see cref="IDisposable"/>:
/// quem cria usa <c>using</c> e o buffer volta ao pool no <see cref="Dispose"/>.
/// </summary>
public sealed class BufferDeEtiquetas : IDisposable
{
    private readonly ArrayPool<char> _pool;
    private char[]? _buffer;
    private int _posicao;

    /// <summary>Aluga o buffer inicial do pool (padrão: <see cref="ArrayPool{T}.Shared"/>).</summary>
    public BufferDeEtiquetas(int capacidadeInicial = 64, ArrayPool<char>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacidadeInicial, 1);
        _pool = pool ?? ArrayPool<char>.Shared;
        _buffer = _pool.Rent(capacidadeInicial);
    }

    /// <summary>Quantidade de códigos adicionados.</summary>
    public int Quantidade { get; private set; }

    /// <summary>
    /// Conteúdo acumulado até agora, sem copiar. Não guarde esse span depois do Dispose:
    /// o array volta ao pool e pode ser reutilizado por outro código.
    /// </summary>
    public ReadOnlySpan<char> Conteudo
    {
        get
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);
            return _buffer.AsSpan(0, _posicao);
        }
    }

    /// <summary>
    /// Escreve o código + '\n'. Se não couber, aluga um array com o dobro do tamanho (ou o necessário),
    /// copia o conteúdo e DEVOLVE o antigo ao pool.
    /// Lança <see cref="ObjectDisposedException"/> se usado depois do Dispose.
    /// </summary>
    public void Adicionar(CodigoPedido codigo)
    {
        ObjectDisposedException.ThrowIf(_buffer is null, this);

        var necessario = _posicao + CodigoPedido.Tamanho + 1;
        if (necessario > _buffer.Length)
        {
            var novo = _pool.Rent(Math.Max(_buffer.Length * 2, necessario));
            _buffer.AsSpan(0, _posicao).CopyTo(novo);
            _pool.Return(_buffer);
            _buffer = novo;
        }

        ParserCodigoPedido.TryFormat(codigo, _buffer.AsSpan(_posicao), out var escritos);
        _posicao += escritos;
        _buffer[_posicao++] = '\n';
        Quantidade++;
    }

    /// <summary>
    /// Devolve o buffer ao pool. Idempotente: chamar duas vezes devolve uma vez só
    /// (devolver o mesmo array duas vezes corromperia o pool).
    /// </summary>
    public void Dispose()
    {
        if (_buffer is null)
            return;

        _pool.Return(_buffer);
        _buffer = null;
    }
}
