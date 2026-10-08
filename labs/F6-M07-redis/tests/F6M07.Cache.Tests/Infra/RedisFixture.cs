using System.Collections.Concurrent;
using System.Diagnostics;
using F6M07.Cache.Catalogo;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace F6M07.Cache.Tests.Infra;

/// <summary>
/// Sobe UM Redis para o assembly de testes inteiro e abre UMA conexão (ConnectionMultiplexer é caro e
/// thread-safe: um por processo). Pronto: leia, não altere. Os testes usam ids/clientes/instâncias
/// únicos, então não precisam limpar o banco entre si.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    /// <summary>Imagem fixa: a mesma no seu PC e no CI.</summary>
    public const string Imagem = "redis:7.4-alpine";

    private readonly RedisContainer _container = new RedisBuilder(Imagem).Build();

    public string ConnectionString { get; private set; } = "";

    public IConnectionMultiplexer Conexao { get; private set; } = null!;

    public IDatabase Db => Conexao.GetDatabase();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Conexao = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (Conexao is not null) await Conexao.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoRedis : ICollectionFixture<RedisFixture>
{
    public const string Nome = "Redis";
}

/// <summary>
/// Fonte fake que CONTA os acessos (é assim que provamos que o cache funciona) e simula a latência do banco.
/// </summary>
public sealed class FonteContadora : IFonteDeProdutos
{
    private readonly ConcurrentDictionary<Guid, Produto> _produtos = new();
    private int _leituras;

    /// <summary>Latência simulada de cada leitura (um SELECT com JOINs no SQL Server).</summary>
    public TimeSpan Latencia { get; init; } = TimeSpan.Zero;

    public int Leituras => Volatile.Read(ref _leituras);

    public Produto Adicionar(string nome = "Teclado mecânico", decimal preco = 349.90m)
    {
        var p = new Produto(Guid.NewGuid(), nome, preco, "Periféricos", true);
        _produtos[p.Id] = p;
        return p;
    }

    public async Task<Produto?> ObterAsync(Guid id, CancellationToken ct)
    {
        Interlocked.Increment(ref _leituras);
        if (Latencia > TimeSpan.Zero) await Task.Delay(Latencia, ct);
        return _produtos.GetValueOrDefault(id);
    }

    public Task AtualizarAsync(Produto produto, CancellationToken ct)
    {
        _produtos[produto.Id] = produto;
        return Task.CompletedTask;
    }
}

public static class Apoio
{
    /// <summary>Polling com timeout curto (nada de Task.Delay fixo "torcendo").</summary>
    public static async Task Eventualmente(Func<Task<bool>> condicao, string descricao, TimeSpan? timeout = null)
    {
        var limite = timeout ?? TimeSpan.FromSeconds(10);
        var relogio = Stopwatch.StartNew();
        while (relogio.Elapsed < limite)
        {
            if (await condicao()) return;
            await Task.Delay(10);
        }
        throw new TimeoutException($"Não aconteceu em {limite.TotalSeconds:0} s: {descricao}");
    }
}
