using F3M07.Dados.Modelo;
using F3M07.Dados.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M07.Dados.Concorrencia;

/// <summary>Resultado de uma alteração com concorrência otimista (o que uma API traduziria em 200/409/404).</summary>
public abstract record ResultadoAlteracao
{
    private ResultadoAlteracao() { }

    /// <summary>Gravou. <paramref name="NovaVersao"/> vira o próximo ETag/If-Match do cliente.</summary>
    public sealed record Ok(byte[] NovaVersao) : ResultadoAlteracao;

    /// <summary>Alguém alterou antes (HTTP 409/412). <paramref name="VersaoAtual"/> permite ao cliente recarregar.</summary>
    public sealed record Conflito(byte[] VersaoAtual) : ResultadoAlteracao;

    /// <summary>O pedido não existe (ou foi apagado no meio do caminho) — HTTP 404.</summary>
    public sealed record NaoEncontrado : ResultadoAlteracao;
}

/// <summary>
/// Duas estratégias de concorrência otimista sobre o <c>rowversion</c> de <see cref="Pedido.Versao"/>.
/// </summary>
public sealed class AlteracaoDePedidos(LojaDbContext db)
{
    /// <summary>
    /// Passo 5 — estratégia "devolver conflito" (409): o cliente informa a versão que ELE leu
    /// (ex.: header If-Match). Se o pedido mudou desde então, nada é gravado e o resultado é
    /// <see cref="ResultadoAlteracao.Conflito"/> com a versão atual do banco.
    /// </summary>
    public Task<ResultadoAlteracao> CancelarAsync(int pedidoId, byte[] versaoLida, CancellationToken ct = default)
    {
        _ = db;
        throw new NotImplementedException(
            "TODO Passo 5: carregue o pedido (null → NaoEncontrado); db.Entry(pedido).Property(p => p.Versao).OriginalValue = versaoLida; " +
            "pedido.Cancelar(); SaveChangesAsync → Ok(pedido.Versao). Em DbUpdateConcurrencyException: " +
            "GetDatabaseValuesAsync() da entrada → null = NaoEncontrado, senão Conflito(valores.GetValue<byte[]>(\"Versao\")).");
    }

    /// <summary>
    /// Passo 6 — estratégia "recarregar e reaplicar": a operação é comutativa o bastante para ser refeita
    /// sobre o valor novo (desconto sobre o total ATUAL). Em conflito, recarrega a entidade do banco e
    /// reaplica, até <paramref name="maxTentativas"/>. Depois disso, deixa a
    /// <see cref="DbUpdateConcurrencyException"/> subir. Pedido inexistente: <see cref="KeyNotFoundException"/>.
    /// </summary>
    public Task<Pedido> AplicarDescontoAsync(int pedidoId, decimal percentual, int maxTentativas = 3, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 6: carregue o pedido e faça um loop de tentativas: pedido.AplicarDesconto(percentual) → SaveChangesAsync. " +
            "catch (DbUpdateConcurrencyException ex) when (tentativa < maxTentativas) { foreach entrada em ex.Entries: await entrada.ReloadAsync(ct); }");
}
