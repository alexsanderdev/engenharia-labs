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
    public async Task<ResultadoReserva> ReservarOtimistaAsync(
        int produtoId, int quantidade, int maxTentativas = 3,
        Func<Task>? entreLeituraEEscrita = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTentativas, 1);

        await using var db = EstoqueDbContext.Criar(connectionString);
        var produto = await db.Produtos.SingleAsync(p => p.Id == produtoId, ct);

        for (var tentativa = 1; ; tentativa++)
        {
            if (entreLeituraEEscrita is not null) await entreLeituraEEscrita();

            if (produto.Estoque < quantidade) return ResultadoReserva.EstoqueInsuficiente;
            produto.Estoque -= quantidade;

            try
            {
                await db.SaveChangesAsync(ct);
                return ResultadoReserva.Reservado;
            }
            catch (DbUpdateConcurrencyException ex) when (tentativa < maxTentativas)
            {
                // Armadilha: consultar de novo com db.Produtos.Single(...) NÃO atualiza a entidade
                // rastreada (identity resolution devolve a instância da memória, com Estoque e
                // Versao velhos). ReloadAsync sobrescreve valores atuais e originais com os do banco.
                foreach (var entrada in ex.Entries)
                    await entrada.ReloadAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoReserva.Conflito;
            }
        }
    }

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
    public async Task<int?> CriarPedidoAsync(int clienteId, int produtoId, int quantidade, CancellationToken ct = default)
    {
        await using var db = EstoqueDbContext.Criar(connectionString);

        // ExecuteUpdate não passa pelo change tracker e NÃO participa da transação implícita do
        // SaveChanges: sem esta transação explícita, ele faz autocommit sozinho.
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var linhas = await db.Produtos
            .Where(p => p.Id == produtoId && p.Estoque >= quantidade)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estoque, p => p.Estoque - quantidade), ct);

        if (linhas == 0) return null; // o DisposeAsync da transação faz rollback

        var pedido = new Pedido { ClienteId = clienteId, ProdutoId = produtoId, Quantidade = quantidade };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct); // se lançar, o DisposeAsync da transação faz rollback

        await transacao.CommitAsync(ct);
        return pedido.Id;
    }
}
