# MP3 — Worker de Notificações

**Mini-projeto:** MP3 (Fase 6 — Sistemas Distribuídos) · **Tempo:** ~12 h distribuídas na fase

Quando um pedido é criado no OrderFlow, a API publica `PedidoCriado`. Este worker notifica o cliente (e-mail/SMS simulados).
A notificação **não pode atrasar a API, não pode ser perdida e não pode ser enviada duas vezes.**

```bash
dotnet test projetos/MP3-worker-notificacoes
```

Precisa do Docker rodando. Os testes sobem, uma vez por execução, o **emulador do Azure Service Bus**
(`mcr.microsoft.com/azure-messaging/servicebus-emulator:latest`) e um **SQL Server 2022** (que serve ao emulador e à sua inbox),
via `Testcontainers.ServiceBus` + `Testcontainers.MsSql`, em rede própria e portas aleatórias. Subida: ~20 s.

Este é um **projeto aberto**: os 2 testes de exemplo já passam. Não existe "deixar verde"; a régua são os
**critérios de aceite** (abaixo), e cada um vira um teste de integração seu.

## O que vem pronto

| Onde | O quê |
|---|---|
| `src/MP3.Contratos` | O contrato `PedidoCriado` (com `EventoId` = chave de idempotência) e `ConvencoesDeMensagem` (fila, `Subject`, `traceparent`, JSON). Sem dependência do SDK. |
| `src/MP3.Notificacoes.Worker` | Esqueleto de Worker Service (`Worker.cs` só loga), `appsettings*.json`. |
| `infra/servicebus/Config.json` | Topologia do emulador: fila `notificacoes-pedidos`, `MaxDeliveryCount = 5`, `LockDuration = 30 s`, DLQ. |
| `infra/docker-compose.yml` | Ambiente de dev: emulador + SQL Server + smtp4dev (senha do SQL vem de variável de ambiente). |
| `tests/.../Infra/AmbienteFixture.cs` | Containers prontos: `ServiceBus` (connection string), `Inbox` (banco `Mp3Notificacoes` já criado, vazio), `Cliente`, `DrenarAsync()`. |
| `tests/.../Infra/Esperas.cs` | `Eventualmente(...)` (polling com timeout) e `Sinal(...)` (TaskCompletionSource). **Nunca** `Task.Delay` fixo esperando mensagem. |
| `tests/.../Infra/Novo.cs` | `Novo.Pedido()` e `Novo.Mensagem(evento, traceparent)` — a mensagem como a API publica. |
| `tests/.../ExemplosTests.cs` | Round-trip de `PedidoCriado` pelo emulador e o banco da inbox no ar. |

## Missão (nesta ordem)

1. **Consumidor.** Troque o `Worker` por um `IHostedService` com `ServiceBusProcessor` (peek-lock, `AutoCompleteMessages = false`).
   Toda mensagem termina com UMA decisão explícita: complete, abandon ou dead-letter. Um `ServiceBusClient` por processo.
2. **Inbox (idempotência).** Tabela no SQL Server com chave `(Consumidor, EventoId)`. Abra transação → insira → envie → commit.
   Violação de PK = duplicata → complete sem enviar. Pense no que acontece com duas cópias **concorrentes** e com
   "enviou e caiu antes do commit".
3. **Notificadores plugáveis (Strategy).** `INotificador { Canal; EnviarAsync }` + um seletor por canal preferido com canal padrão:
   console, **SMTP fake** (`SmtpClient` com `SpecifiedPickupDirectory` gravando `.eml` — ou smtp4dev do compose) e **SMS fake** (valida E.164).
4. **Falhas.** Classifique: permanente (destino inválido, contrato ilegível) → **DLQ** com motivo; transitória → **retry com backoff
   exponencial + jitter** (dica: reagende uma cópia com `ScheduleMessageAsync` e um contador de tentativas; esgotou → DLQ).
   Crie o comando `dotnet run -- reprocessar-dlq [maximo]` que devolve a DLQ para a fila.
5. **Graceful shutdown.** No `StopAsync`, `StopProcessingAsync` (para de receber e espera os handlers). Handler interrompido →
   abandon + rollback da inbox. Configure `HostOptions.ShutdownTimeout` menor que o prazo do orquestrador.
6. **Health checks.** `/health/live` (sem dependências) e `/health/ready` (inbox + consumidor processando). Health checks vêm do
   ASP.NET Core: trocar o SDK do worker para `Microsoft.NET.Sdk.Web` é uma decisão aceitável — registre no ADR.
7. **ADR: Service Bus vs Kafka** para este caso (use o template de ADR do vault).
8. **(Opcional — a Fase 8 aprofunda) Telemetria.** `ActivitySource` próprio; o span de processamento continua o trace da API
   lendo o `traceparent` da mensagem (`ActivityContext.TryParse`). Para exportar, adicione os pacotes `OpenTelemetry.Extensions.Hosting`
   e `OpenTelemetry.Exporter.OpenTelemetryProtocol` ao `Directory.Packages.props` (decisão sua) e veja API → Service Bus → Worker
   num único trace (Aspire Dashboard ou Jaeger).

## Critérios de aceite (cada um é um teste seu)

- [ ] A mesma mensagem entregue 2× gera **1** notificação (e cópias concorrentes também).
- [ ] Falha transitória → retry → sucesso; falha permanente → DLQ (com motivo); tentativas esgotadas → DLQ.
- [ ] Parar o worker no meio do processamento **não perde** a mensagem (outra instância a processa, uma vez).
- [ ] Trace mostra API → Service Bus → Worker numa única visualização (no teste: mesmo `TraceId`, `ParentSpanId` = span da API).
- [ ] ADR: Service Bus vs Kafka.

> [!TIP]
> Testes com broker compartilham a fila. Sempre **filtre pelos ids do próprio teste** (o `EventoId` é novo a cada `Novo.Pedido()`):
> uma mensagem reagendada por um teste anterior pode aparecer no meio do seu. E nada de `Task.Delay(2000)` "para dar tempo".

## Por que o emulador do Service Bus (e não RabbitMQ)?

O MP3 pede Service Bus, e o emulador oficial se mostrou estável aqui: sobe em ~20 s com o SQL Server 2022 (o mesmo da inbox),
suporta peek-lock, DLQ com motivo, `MaxDeliveryCount` e mensagens agendadas — tudo de que o MP3 precisa — com o mesmo SDK de produção.
Limitações conhecidas: a topologia vem do `Config.json` (não dá para criar filas pelo `ServiceBusAdministrationClient`),
há limite baixo de conexões simultâneas (use um `ServiceBusClient` só) e não há cotas/throttling realistas.

## Rodando o worker localmente

```powershell
cd projetos/MP3-worker-notificacoes/infra
$env:MSSQL_SA_PASSWORD = "<uma senha forte sua>"
docker compose up -d
docker compose exec sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -Q "CREATE DATABASE Mp3Notificacoes"
cd ..
$env:ConnectionStrings__Inbox = "Server=localhost,1433;Database=Mp3Notificacoes;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;TrustServerCertificate=True"
dotnet run --project src/MP3.Notificacoes.Worker
```

A connection string do emulador (`appsettings.Development.json`) é a pública e fixa do emulador — não é segredo.
A do SQL **nunca** vai para arquivo: variável de ambiente ou `dotnet user-secrets`.

## Gabarito

Na branch `solucoes`: worker completo (`Mensageria/` com consumidor, política de retry e reprocessador de DLQ; `Inbox/`;
`Notificacoes/` com Strategy e três provedores; `Saude/`; `Telemetria/`), ADR em `docs/adr/` e 21 testes
(idempotência com cópias concorrentes, retry → sucesso, permanente → DLQ, tentativas esgotadas, contrato inválido,
reprocessar DLQ, shutdown no meio do envio, health checks, trace continuando o da API e testes de unidade).
Compare **depois** de terminar.
