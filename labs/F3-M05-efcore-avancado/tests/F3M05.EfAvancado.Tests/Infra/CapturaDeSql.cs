using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F3M05.EfAvancado.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTO. Interceptador de comandos que guarda o texto de CADA comando SQL que o EF
/// envia ao banco. Cada comando é um round-trip: é assim que os testes medem N+1,
/// explosão cartesiana, lotes etc.
/// </summary>
public sealed class CapturaDeSql : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _comandos = new();

    /// <summary>Comandos executados desde o último <see cref="Limpar"/>, em ordem.</summary>
    public IReadOnlyList<string> Comandos => [.. _comandos];

    public void Limpar() => _comandos.Clear();

    /// <summary>Texto de todos os comandos, para mensagens de erro legíveis.</summary>
    public string Relatorio() =>
        string.Join(Environment.NewLine + "---" + Environment.NewLine, _comandos.Select((c, i) => $"#{i + 1}{Environment.NewLine}{c}"));

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        _comandos.Enqueue(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        _comandos.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        _comandos.Enqueue(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _comandos.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        _comandos.Enqueue(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        _comandos.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
