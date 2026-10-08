# F6-M06 — Workers e BackgroundService

**Módulo:** Workers e BackgroundService (Fase 6)
**Tempo:** 2–3 horas
**Sem Docker e sem broker:** o foco é o host. A "fila" é um `Channel<T>` em memória e as bordas (relógio, repositório, enviador) são fakes.

Um Worker Service do OrderFlow com dois `BackgroundService`:

- **Expiração de pedidos não pagos**: `PeriodicTimer` + `TimeProvider`, serviço scoped resolvido por ciclo com `IServiceScopeFactory`, falha isolada que não mata o loop, desistência após N falhas seguidas (o host para por `BackgroundServiceExceptionBehavior.StopHost`, e o `Program` devolve exit code ≠ 0), batimento registrado a cada ciclo e logging estruturado com `[LoggerMessage]`.
- **Processador de notificações**: `Channel<T>` limitado (backpressure), N leitores em paralelo e **desligamento gracioso** — ao parar, não aceita itens novos, termina o que está em andamento, não começa itens novos e só interrompe o trabalho em andamento quando o `HostOptions.ShutdownTimeout` estoura.
- **Health check** do worker por batimento: recente → Healthy; ciclo falhando → Degraded; travado, morto ou sem nenhum ciclo → Unhealthy.

Os testes sobem o **host real** (`Host.CreateApplicationBuilder`, ambiente Development, então `ValidateScopes` e `ValidateOnBuild` estão ligados) e controlam o tempo com um `FakeTimeProvider` (`Infra/RelogioDeTeste`). A única espera real é o `ShutdownTimeout` de 300 ms de um teste.

```bash
dotnet test labs/F6-M06-workers
# ou só o projeto de testes:
dotnet test labs/F6-M06-workers/tests/F6M06.Worker.Tests
```

A suíte roda em menos de 1 s.

## O que você completa (nesta ordem)

1. `DependencyInjection.cs` — `AddWorkersDoOrderFlow` (options validadas, lifetimes, hosted services, health check).
2. `Expiracao/Expiracao.cs` — `ServicoDeExpiracao.ExpirarAsync`.
3. `Expiracao/ExpiracaoDePedidosWorker.cs` — `ExecuteAsync` e `ExecutarCicloAsync` (timer, escopo por ciclo, log, batimento).
4. O mesmo arquivo — falha isolada × falhas seguidas (`LogDesistindo`, `RegistrarFalhaFatal`, relançar).
5. `Saude/WorkerHealthCheck.cs` — regras do batimento.
6. (sem código novo) `SaudeTests`: worker travado e `BackgroundServiceExceptionBehavior.Ignore`.
7. `Notificacoes/FilaDeNotificacoes.cs` — `Channel.CreateBounded` com `FullMode = Wait`.
8. `Notificacoes/ProcessadorDeNotificacoesWorker.cs` — `ExecuteAsync`, `LerAsync`, `ProcessarAsync`.
9. O mesmo arquivo — `StopAsync` respeitando o `ShutdownTimeout`.

No início, os **14 testes falham**. O lab termina quando todos ficam verdes **sem alterar os testes**. Rode a suíte várias vezes seguidas: nenhum teste pode ser intermitente.

Já vêm prontos: `Pedidos/` (domínio e repositório em memória), `Saude/MonitorDeWorkers.cs`, as options, os `[LoggerMessage]`, o `Program.cs` e toda a infra de teste (`tests/.../Infra/`).

Roteiro completo e dicas: nota "Workers e BackgroundService - Lab" no vault do curso. Gabarito na branch `solucoes`.
