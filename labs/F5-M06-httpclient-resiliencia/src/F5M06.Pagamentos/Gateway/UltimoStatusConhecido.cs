using System.Collections.Concurrent;

namespace F5M06.Pagamentos.Gateway;

/// <summary>
/// Memória do último status visto por transação, usada como fallback da consulta.
/// Singleton: o typed client é transiente, então o estado NÃO pode morar nele.
/// (Em produção: IMemoryCache/HybridCache com expiração, ou a própria tabela de pagamentos.)
/// </summary>
public sealed class UltimoStatusConhecido
{
    private readonly ConcurrentDictionary<string, StatusPagamento> _status = new();

    public void Registrar(string transacaoId, StatusPagamento status) => _status[transacaoId] = status;

    public bool TentarObter(string transacaoId, out StatusPagamento status) => _status.TryGetValue(transacaoId, out status);
}
