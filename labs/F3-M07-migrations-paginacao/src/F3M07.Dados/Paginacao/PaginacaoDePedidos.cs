using F3M07.Dados.Modelo;
using F3M07.Dados.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M07.Dados.Paginacao;

/// <summary>Linha da listagem de pedidos.</summary>
public sealed record PedidoResumo(int Id, int ClienteId, DateTime CriadoEm, StatusPedido Status, decimal Total);

/// <summary>Página keyset: os itens e o cursor para pedir a próxima (<c>null</c> = acabou).</summary>
public sealed record PaginaKeyset<T>(IReadOnlyList<T> Itens, string? ProximoCursor);

/// <summary>
/// Listagem de pedidos do mais recente para o mais antigo — ORDER BY CriadoEm DESC, Id DESC —
/// de dois jeitos: OFFSET (número de página) e KEYSET (cursor).
/// </summary>
public sealed class PaginacaoDePedidos(LojaDbContext db)
{
    public const int TamanhoMaximo = 100;

    /// <summary>
    /// Passo 8: consulta OFFSET/FETCH da página <paramref name="numeroPagina"/> (começa em 1).
    /// Devolve <see cref="IQueryable{T}"/> para os testes poderem inspecionar o SQL (ToQueryString).
    /// </summary>
    public IQueryable<PedidoResumo> ConsultaPorOffset(int numeroPagina, int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroPagina, 1);
        ValidarTamanho(tamanho);

        return db.Pedidos
            .AsNoTracking()
            .OrderByDescending(p => p.CriadoEm)
            .ThenByDescending(p => p.Id)
            .Skip((numeroPagina - 1) * tamanho)
            .Take(tamanho)
            .Select(p => new PedidoResumo(p.Id, p.ClienteId, p.CriadoEm, p.Status, p.Total));
    }

    /// <summary>
    /// Passo 9: consulta KEYSET: os <paramref name="tamanho"/> pedidos imediatamente DEPOIS de
    /// <paramref name="depoisDe"/> na ordenação (CriadoEm DESC, Id DESC); <c>null</c> = primeira página.
    /// O predicado precisa ser "buscável" pelo índice (CriadoEm, Id).
    /// </summary>
    public IQueryable<PedidoResumo> ConsultaPorKeyset(PosicaoCursor? depoisDe, int tamanho)
    {
        ValidarTamanho(tamanho);
        return MontarKeyset(depoisDe, tamanho);
    }

    private IQueryable<PedidoResumo> MontarKeyset(PosicaoCursor? depoisDe, int quantidade)
    {
        var consulta = db.Pedidos.AsNoTracking();

        if (depoisDe is { } c)
        {
            // "(CriadoEm, Id) < (c.CriadoEm, c.Id)" escrito de forma que o otimizador faça SEEK:
            // o primeiro termo (CriadoEm <= x) delimita a faixa do índice; o segundo resolve os empates.
            consulta = consulta.Where(p =>
                p.CriadoEm <= c.CriadoEm &&
                (p.CriadoEm < c.CriadoEm || p.Id < c.Id));
        }

        return consulta
            .OrderByDescending(p => p.CriadoEm)
            .ThenByDescending(p => p.Id)
            .Take(quantidade)
            .Select(p => new PedidoResumo(p.Id, p.ClienteId, p.CriadoEm, p.Status, p.Total));
    }

    public async Task<IReadOnlyList<PedidoResumo>> ListarPorOffsetAsync(int numeroPagina, int tamanho, CancellationToken ct = default) =>
        await ConsultaPorOffset(numeroPagina, tamanho).ToListAsync(ct);

    /// <summary>
    /// Passo 9: página keyset a partir de um cursor opaco (<c>null</c> = primeira página).
    /// Busca <c>tamanho + 1</c> linhas para saber se existe próxima página sem um COUNT(*).
    /// </summary>
    public async Task<PaginaKeyset<PedidoResumo>> ListarPorKeysetAsync(string? cursor, int tamanho, CancellationToken ct = default)
    {
        ValidarTamanho(tamanho);
        PosicaoCursor? depoisDe = cursor is null ? null : CursorDePaginacao.Decodificar(cursor);

        var linhas = await MontarKeyset(depoisDe, tamanho + 1).ToListAsync(ct);
        if (linhas.Count <= tamanho)
            return new PaginaKeyset<PedidoResumo>(linhas, null);

        var itens = linhas.Take(tamanho).ToList();
        var ultimo = itens[^1];
        return new PaginaKeyset<PedidoResumo>(itens, CursorDePaginacao.Codificar(new PosicaoCursor(ultimo.CriadoEm, ultimo.Id)));
    }

    private static void ValidarTamanho(int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanho, TamanhoMaximo);
    }
}
