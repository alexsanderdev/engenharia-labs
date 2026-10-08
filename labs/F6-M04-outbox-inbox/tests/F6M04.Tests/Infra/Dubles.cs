using System.Collections.Concurrent;
using System.Data.Common;
using F6M04.Pedidos.Mensageria;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F6M04.Tests.Infra;

/// <summary>PRONTO. Exceção das falhas simuladas pelos dublês (fácil de reconhecer nas asserções).</summary>
public sealed class FalhaSimuladaException(string mensagem) : Exception(mensagem);

/// <summary>
/// PRONTO. Publicador em memória: grava o que foi publicado, na ordem. Opcionalmente FALHA para algumas mensagens
/// (a função devolve a exceção a lançar, ou <c>null</c> para publicar).
/// </summary>
public sealed class PublicadorQueGrava(Func<MensagemDeSaida, int, Exception?>? falhar = null) : IPublicadorDeMensagens
{
    private readonly ConcurrentQueue<MensagemDeSaida> _publicadas = new();
    private readonly ConcurrentDictionary<Guid, int> _tentativas = new();

    /// <summary>Mensagens publicadas COM sucesso, na ordem.</summary>
    public IReadOnlyList<MensagemDeSaida> Publicadas => [.. _publicadas];

    /// <summary>Quantas vezes cada MessageId foi oferecido ao publicador (sucesso ou falha).</summary>
    public int TentativasDe(Guid messageId) => _tentativas.GetValueOrDefault(messageId);

    public Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default)
    {
        var tentativa = _tentativas.AddOrUpdate(mensagem.MessageId, 1, (_, n) => n + 1);
        if (falhar?.Invoke(mensagem, tentativa) is { } erro) throw erro;
        _publicadas.Enqueue(mensagem);
        return Task.CompletedTask;
    }
}

/// <summary>PRONTO. Broker fora do ar: toda publicação falha.</summary>
public sealed class PublicadorForaDoAr : IPublicadorDeMensagens
{
    public Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default) =>
        throw new IOException("Simulação: broker fora do ar (connection refused).");
}

/// <summary>PRONTO. Na PRIMEIRA publicação, para no <see cref="PontoDeParada"/> até o teste liberar; depois delega.</summary>
public sealed class PublicadorComPortao(IPublicadorDeMensagens interno, PontoDeParada portao) : IPublicadorDeMensagens
{
    public async Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default)
    {
        await portao.Gancho();
        await interno.PublicarAsync(mensagem, ct);
    }
}

/// <summary>
/// PRONTO. Interceptor de comandos do EF Core que faz FALHAR os comandos SQL que casam com o filtro, as primeiras
/// <paramref name="vezes"/> vezes. Simula "o banco caiu / o processo morreu" num ponto exato.
/// </summary>
public sealed class FalharComandosInterceptor(Func<string, bool> filtro, int vezes = int.MaxValue, string motivo = "falha simulada")
    : DbCommandInterceptor
{
    private int _restantes = vezes;

    public int Falhas { get; private set; }

    private void TalvezFalhar(DbCommand comando)
    {
        if (!filtro(comando.CommandText)) return;
        if (Interlocked.Decrement(ref _restantes) < 0) return;
        Falhas++;
        throw new FalhaSimuladaException($"Simulação: {motivo}.");
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        TalvezFalhar(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        TalvezFalhar(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        TalvezFalhar(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        TalvezFalhar(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        TalvezFalhar(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        TalvezFalhar(command);
        return ValueTask.FromResult(result);
    }

    /// <summary>A exceção (ou alguma interna dela) veio deste interceptor?</summary>
    public static bool VeioDaSimulacao(Exception? ex)
    {
        for (var atual = ex; atual is not null; atual = atual.InnerException)
            if (atual is FalhaSimuladaException) return true;
        return false;
    }
}
