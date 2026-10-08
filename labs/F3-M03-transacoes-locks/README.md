# F3-M03 — Transações, Isolamento e Locks

**Módulo:** Transações, Isolamento e Locks (Fase 3)
**Tempo:** 2–3 horas
**Requer Docker** (Testcontainers sobe um SQL Server 2022; imagem `mcr.microsoft.com/mssql/server:2022-latest`).

Estoque do OrderFlow sob concorrência, com ADO.NET (`Microsoft.Data.SqlClient`) e um pouco de EF Core. Cada cenário é reproduzido de forma **determinística** com duas conexões coordenadas: um "ponto de parada" pausa a transação A num lugar exato, a sessão B age, e o teste observa `sys.dm_exec_requests` para saber quando alguém está realmente bloqueado. Nada de `Thread.Sleep` e torcer.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez
dotnet test labs/F3-M03-transacoes-locks
```

Com a imagem já baixada, a suíte roda em ~20–30 s (o container sobe uma vez; os testes de deadlock esperam o lock monitor, que detecta o ciclo em até ~5 s).

## O que já vem pronto (e verde)

- `tests/.../Infra/` — container, dois bancos (`F3M03_Locking` com READ COMMITTED clássico e `F3M03_Rcsi` com `READ_COMMITTED_SNAPSHOT ON`), reset dos dados antes de cada teste, `Sessao` (uma "aba do SSMS"), `PontoDeParada` e o observador de bloqueios.
- `tests/.../Demonstracoes/` — **15 testes de demonstração**: dirty read, non-repeatable read, phantom, SERIALIZABLE, SNAPSHOT (e o erro 3960), RCSI × locking, lost update, deadlock com `DEADLOCK_PRIORITY` (erro 1205) e `LOCK_TIMEOUT` (erro 1222) com e sem `XACT_ABORT`. Leia como material de aula.
- No `src`: `ReservaDeEstoque.ReservarIngenuoAsync` (o bug), `TransferenciaDeEstoque.MoverNaOrdemDoPedidoAsync` (o deadlock), `EstoqueDbContext` e o C# de `DiagnosticoDeBloqueio`.

## O que você completa (nesta ordem)

1. `Estoque/ReservaDeEstoque.cs` — `ReservarAtomicoAsync` (UPDATE condicional).
2. `ReservarComUpdLockAsync` (`WITH (UPDLOCK, HOLDLOCK)`).
3. `ReservarOtimistaAsync` (`rowversion` + retry).
4. `Estoque/RelatorioDeEstoque.cs` — `GerarAsync` consistente sem bloquear escritores.
5. `Deadlocks/PoliticaDeRetry.cs` e `Deadlocks/RetryDeDeadlock.cs` — backoff exponencial e retry da vítima (1205).
6. `Deadlocks/TransferenciaDeEstoque.cs` — `MoverEmOrdemConsistenteAsync`.
7. `Sql/QuemBloqueiaQuem.sql` e `Sql/LocksDaSessao.sql` — diagnóstico com DMVs.
8. `Diagnostico/ConsultaDeEstoque.cs` — `SET LOCK_TIMEOUT` e erro 1222.
9. `EfCore/EstoqueComEfCore.cs` — `ReservarOtimistaAsync` com `DbUpdateConcurrencyException`.
10. `CriarPedidoAsync` — `BeginTransaction` + `ExecuteUpdate` + `SaveChanges` atômicos.

**Não altere os testes.** No início, 38 testes falham e os 15 de demonstração passam; o lab termina quando os 53 ficam verdes. Rode a suíte algumas vezes seguidas: nenhum teste pode ser intermitente.

Roteiro completo e dicas: nota "Transações, Isolamento e Locks - Lab" no vault do curso. Gabarito na branch `solucoes`.
