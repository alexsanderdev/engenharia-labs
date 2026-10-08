# F1-M03 — Async/await e Cancelamento

**Módulo:** Async Await e Cancelamento (Fase 1)
**Tempo:** 2h

Concorrência controlada com `SemaphoreSlim` + `Task.WhenAll` (sem perder exceções), timeout com `CancellationTokenSource` + `TimeProvider` (testado com `FakeTimeProvider`), `ValueTask` no caminho quente de um cache, produtor/consumidor com `Channel<T>` que não trava quando um consumidor falha e leitura paginada com `IAsyncEnumerable<T>` + `[EnumeratorCancellation]`.

```bash
dotnet test labs/F1-M03-async-cancelamento/tests/F1M03.Async.Tests
```

1. `ProcessadorEmLote.cs` (`ProcessadorEmLoteTests`).
2. `ExecutorComTimeout.cs` (`ExecutorComTimeoutTests`).
3. `CacheDePrecos.cs` (`CacheDePrecosTests`).
4. `PipelineComChannel.cs` (`PipelineComChannelTests`).
5. `LeitorPaginado.cs` (`LeitorPaginadoTests`).

Os testes são determinísticos: o tempo é controlado por `FakeTimeProvider` (pacote `Microsoft.Extensions.TimeProvider.Testing`) e nenhum teste depende de `Thread.Sleep`. **Não altere os testes.** Roteiro completo na nota `Async Await e Cancelamento - Lab` do vault. Gabarito na branch `solucoes`.
