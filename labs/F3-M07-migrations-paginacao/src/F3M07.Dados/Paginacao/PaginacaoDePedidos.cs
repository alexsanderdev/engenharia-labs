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
        _ = db;
        throw new NotImplementedException(
            "TODO Passo 8: valide (numeroPagina >= 1; 1 <= tamanho <= TamanhoMaximo); AsNoTracking, " +
            "OrderByDescending(CriadoEm).ThenByDescending(Id), Skip((numeroPagina - 1) * tamanho), Take(tamanho), Select(new PedidoResumo(...)).");
    }

    /// <summary>
    /// Passo 9: consulta KEYSET: os <paramref name="tamanho"/> pedidos imediatamente DEPOIS de
    /// <paramref name="depoisDe"/> na ordenação (CriadoEm DESC, Id DESC); <c>null</c> = primeira página.
    /// O predicado precisa ser "buscável" pelo índice (CriadoEm, Id).
    /// </summary>
    public IQueryable<PedidoResumo> ConsultaPorKeyset(PosicaoCursor? depoisDe, int tamanho) =>
        throw new NotImplementedException(
            "TODO Passo 9: com cursor, Where(p => p.CriadoEm <= c.CriadoEm && (p.CriadoEm < c.CriadoEm || p.Id < c.Id)); " +
            "mesma ordenação do offset, Take(tamanho), SEM Skip.");

    public async Task<IReadOnlyList<PedidoResumo>> ListarPorOffsetAsync(int numeroPagina, int tamanho, CancellationToken ct = default) =>
        await ConsultaPorOffset(numeroPagina, tamanho).ToListAsync(ct);

    /// <summary>
    /// Passo 9: página keyset a partir de um cursor opaco (<c>null</c> = primeira página).
    /// Busca <c>tamanho + 1</c> linhas para saber se existe próxima página sem um COUNT(*).
    /// </summary>
    public Task<PaginaKeyset<PedidoResumo>> ListarPorKeysetAsync(string? cursor, int tamanho, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 9: decodifique o cursor (null = primeira página) e busque tamanho + 1 linhas; se vierem mais que 'tamanho', " +
            "devolva só 'tamanho' itens e o cursor do ÚLTIMO item devolvido; senão ProximoCursor = null.");
}
