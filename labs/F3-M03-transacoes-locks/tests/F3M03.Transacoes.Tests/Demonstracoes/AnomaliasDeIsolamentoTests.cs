using F3M03.Transacoes.Tests.Infra;

namespace F3M03.Transacoes.Tests.Demonstracoes;

/// <summary>
/// DEMONSTRAÇÕES (já vêm verdes): cada teste é um roteiro com duas sessões, A e B, como duas abas
/// do SSMS. Leia na ordem; cada um prova uma linha da matriz "anomalia × nível de isolamento".
/// Rode um por vez com o debugger e olhe o banco no meio (sys.dm_tran_locks) para ver os locks.
/// </summary>
public sealed class AnomaliasDeIsolamentoTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    // ------------------------------------------------------------------ dirty read

    [Fact]
    public async Task DirtyRead_ReadUncommitted_EnxergaValorQueDepoisSofreRollback()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Preco = 999.99 WHERE Id = 1;"); // não commitou

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;"); // = WITH (NOLOCK)
        var lidoPorB = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");

        await a.ExecutarAsync("ROLLBACK;");
        var precoReal = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");

        lidoPorB.ShouldBe(999.99m, "B leu um valor que NUNCA existiu de verdade (dirty read)");
        precoReal.ShouldBe(250.00m);
    }

    [Fact]
    public async Task ReadCommittedComLocking_LeitorEsperaOEscritorTerminar()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Preco = 999.99 WHERE Id = 1;"); // X lock na linha

        // READ COMMITTED com locking: B pede S na linha, incompatível com o X de A -> espera.
        var leituraDeB = b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        await EsperarBloqueioDaSessaoAsync(b.Spid);
        leituraDeB.IsCompleted.ShouldBeFalse();

        await a.ExecutarAsync("COMMIT;");
        (await NoMaximoAsync(leituraDeB, "Leitura de B")).ShouldBe(999.99m, "B só leu depois do commit, o valor commitado");
    }

    [Fact]
    public async Task ReadCommittedComRcsi_LeitorLeUltimaVersaoCommitadaSemEsperar()
    {
        var a = await AbrirSessaoAsync("A", rcsi: true);
        var b = await AbrirSessaoAsync("B", rcsi: true);

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Preco = 999.99 WHERE Id = 1;");

        // Mesmo nível (READ COMMITTED), banco com READ_COMMITTED_SNAPSHOT ON: B não pede S lock;
        // lê a última versão COMMITADA da linha no version store (tempdb). Não espera e não lê sujo.
        await b.ExecutarAsync("SET LOCK_TIMEOUT 0;"); // se precisasse esperar 1 ms, falharia com 1222
        var lidoPorB = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");

        await a.ExecutarAsync("COMMIT;");
        lidoPorB.ShouldBe(250.00m);
    }

    // ------------------------------------------------------------------ non-repeatable read

    [Fact]
    public async Task NonRepeatableRead_ReadCommitted_SegundaLeituraVeOutroValor()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL READ COMMITTED; BEGIN TRAN;");
        var primeira = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");

        // Em READ COMMITTED o S lock de B é solto logo após a leitura: A grava sem esperar.
        await a.ExecutarAsync("UPDATE dbo.Produtos SET Preco = 300.00 WHERE Id = 1;");

        var segunda = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        await b.ExecutarAsync("COMMIT;");

        primeira.ShouldBe(250.00m);
        segunda.ShouldBe(300.00m, "mesma transação, mesma linha, valores diferentes (non-repeatable read)");
    }

    [Fact]
    public async Task RepeatableRead_SegundaLeituraIgual_PorqueOEscritorFicaBloqueado()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ; BEGIN TRAN;");
        var primeira = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");

        // REPEATABLE READ segura o S lock até o fim da transação de B: o UPDATE de A espera.
        var updateDeA = a.ExecutarAsync("UPDATE dbo.Produtos SET Preco = 300.00 WHERE Id = 1;");
        await EsperarBloqueioDaSessaoAsync(a.Spid);

        var segunda = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        await b.ExecutarAsync("COMMIT;");
        await NoMaximoAsync(updateDeA, "UPDATE de A");

        primeira.ShouldBe(250.00m);
        segunda.ShouldBe(250.00m, "a leitura se repetiu: quem pagou foi o escritor, que esperou");
        (await EscalarInfraAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;")).ShouldBe(300.00m);
    }

    [Fact]
    public async Task Rcsi_NaoEvitaNonRepeatableRead_CadaComandoTemSuaFoto()
    {
        var a = await AbrirSessaoAsync("A", rcsi: true);
        var b = await AbrirSessaoAsync("B", rcsi: true);

        await b.ExecutarAsync("BEGIN TRAN;"); // READ COMMITTED (com RCSI)
        var primeira = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        await a.ExecutarAsync("UPDATE dbo.Produtos SET Preco = 300.00 WHERE Id = 1;");
        var segunda = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        await b.ExecutarAsync("COMMIT;");

        // RCSI = consistência por COMANDO (statement-level). Cada SELECT vê o último commit
        // existente quando ELE começou. Para a TRANSAÇÃO inteira ver uma foto só: SNAPSHOT.
        primeira.ShouldBe(250.00m);
        segunda.ShouldBe(300.00m);
    }

    // ------------------------------------------------------------------ phantom

    [Fact]
    public async Task Phantom_RepeatableRead_LinhaNovaApareceNaSegundaContagem()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ; BEGIN TRAN;");
        var antes = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");

        // REPEATABLE READ trava as linhas que EXISTIAM; não trava o "espaço" entre elas.
        await a.ExecutarAsync("INSERT dbo.Pedidos (ClienteId, ProdutoId, Quantidade) VALUES (1, 3, 1);");

        var depois = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");
        await b.ExecutarAsync("COMMIT;");

        antes.ShouldBe(2);
        depois.ShouldBe(3, "uma linha 'fantasma' apareceu dentro da mesma transação (phantom read)");
    }

    [Fact]
    public async Task Serializable_InsercaoNoIntervaloLidoFicaBloqueada()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; BEGIN TRAN;");
        var antes = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");

        // SERIALIZABLE pega key-range locks (RangeS-S) no índice IX_Pedidos_ClienteId:
        // o intervalo "ClienteId = 1" fica travado, inclusive para linhas que ainda não existem.
        var insertDeA = a.ExecutarAsync("INSERT dbo.Pedidos (ClienteId, ProdutoId, Quantidade) VALUES (1, 3, 1);");
        await EsperarBloqueioDaSessaoAsync(a.Spid);

        var depois = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");
        await b.ExecutarAsync("COMMIT;");
        await NoMaximoAsync(insertDeA, "INSERT de A");

        antes.ShouldBe(2);
        depois.ShouldBe(2, "sem phantom: o INSERT esperou o fim da transação de B");
        (await EscalarInfraAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;")).ShouldBe(3);
    }

    // ------------------------------------------------------------------ snapshot

    [Fact]
    public async Task Snapshot_LeiturasEstaveisSemBloquearEscritores()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");
        await a.ExecutarAsync("SET LOCK_TIMEOUT 0;"); // A não pode esperar nem 1 ms

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL SNAPSHOT; BEGIN TRAN;");
        // A "foto" é tirada no PRIMEIRO acesso a dados da transação, não no BEGIN TRAN.
        var precoAntes = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        var pedidosAntes = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");

        await a.ExecutarAsync(
            """
            UPDATE dbo.Produtos SET Preco = 300.00 WHERE Id = 1;
            INSERT dbo.Pedidos (ClienteId, ProdutoId, Quantidade) VALUES (1, 3, 1);
            """);

        var precoDepois = await b.EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;");
        var pedidosDepois = await b.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = 1;");
        await b.ExecutarAsync("COMMIT;");

        precoDepois.ShouldBe(precoAntes, "sem non-repeatable read");
        pedidosDepois.ShouldBe(pedidosAntes, "sem phantom");
        (await EscalarInfraAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = 1;")).ShouldBe(300.00m,
            "e o escritor não esperou nada (LOCK_TIMEOUT 0)");
    }

    [Fact]
    public async Task Snapshot_EscritaEmLinhaAlteradaPorOutro_Erro3960EDesfazATransacao()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await b.ExecutarAsync("SET TRANSACTION ISOLATION LEVEL SNAPSHOT; BEGIN TRAN;");
        await b.EscalarAsync<int>("SELECT Estoque FROM dbo.Produtos WHERE Id = 1;"); // foto: 10

        await a.ExecutarAsync("UPDATE dbo.Produtos SET Estoque = Estoque - 3 WHERE Id = 1;"); // commit: 7

        // B tenta gravar numa linha que mudou DEPOIS da foto dele: o SNAPSHOT detecta o conflito
        // (é concorrência otimista embutida no banco) em vez de deixar acontecer um lost update.
        var erro = await b.FalharAsync("UPDATE dbo.Produtos SET Estoque = Estoque - 2 WHERE Id = 1;");

        erro.Number.ShouldBe(3960); // "Snapshot isolation transaction aborted due to update conflict"
        (await b.TranCountAsync()).ShouldBe(0, "a transação de B inteira foi desfeita");
        (await EstoqueAsync(1)).ShouldBe(7);
    }
}
