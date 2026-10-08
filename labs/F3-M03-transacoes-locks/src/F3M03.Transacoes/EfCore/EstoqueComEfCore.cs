using F3M03.Transacoes.Estoque;
using Microsoft.EntityFrameworkCore;

namespace F3M03.Transacoes.EfCore;

/// <summary>Os mesmos problemas de concorrência, agora com EF Core.</summary>
public sealed class EstoqueComEfCore(string connectionString)
{
    /// <summary>
    /// Baixa de estoque com concorrência otimista do EF Core (<c>Versao</c> é <c>IsRowVersion()</c>):
    /// carrega o produto (rastreado), chama <paramref name="entreLeituraEEscrita"/>, verifica o estoque,
    /// subtrai e chama <c>SaveChangesAsync</c>. Em <see cref="DbUpdateConcurrencyException"/>,
    /// RECARREGA os valores do banco e tenta de novo, até <paramref name="maxTentativas"/> no total
    /// (o gancho é chamado em todas). Esgotadas → <see cref="ResultadoReserva.Conflito"/>.
    /// </summary>
    public Task<ResultadoReserva> ReservarOtimistaAsync(
        int produtoId, int quantidade, int maxTentativas = 3,
        Func<Task>? entreLeituraEEscrita = null, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 9): crie o contexto com EstoqueDbContext.Criar(connectionString) e carregue o produto. Em laço: " +
            "gancho, verifique o estoque, subtraia, SaveChangesAsync. Em DbUpdateConcurrencyException: se ainda há " +
            "tentativas, RECARREGUE do banco (ex.Entries -> ReloadAsync) e repita; senão devolva Conflito. " +
            $"(connection string com {connectionString.Length} caracteres)");

    /// <summary>
    /// Cria um pedido E dá baixa no estoque de forma atômica (tudo ou nada):
    /// <list type="number">
    /// <item>abre uma transação explícita (<c>db.Database.BeginTransactionAsync</c>);</item>
    /// <item>baixa o estoque com <c>ExecuteUpdateAsync</c> condicional (<c>Estoque &gt;= quantidade</c>);
    ///   0 linhas → devolve <c>null</c> sem gravar nada;</item>
    /// <item>adiciona o <see cref="Pedido"/> e chama <c>SaveChangesAsync</c>;</item>
    /// <item>faz commit e devolve o Id do pedido.</item>
    /// </list>
    /// Se o <c>SaveChangesAsync</c> falhar (ex.: cliente inexistente → FK), a exceção sobe e a baixa
    /// de estoque NÃO pode ficar gravada.
    /// </summary>
    public Task<int?> CriarPedidoAsync(int clienteId, int produtoId, int quantidade, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 10): BeginTransactionAsync; db.Produtos.Where(p => p.Id == produtoId && p.Estoque >= quantidade)" +
            ".ExecuteUpdateAsync(s => s.SetProperty(p => p.Estoque, p => p.Estoque - quantidade)); 0 linhas -> null; " +
            "senão Add(new Pedido { ... }), SaveChangesAsync, CommitAsync e devolva pedido.Id.");
}
