# F6-M02 — Azure Service Bus

**Módulo:** Azure Service Bus (Fase 6)
**Tempo:** 2–3 horas

O OrderFlow publica `PedidoCriado` num **tópico** e cada consumidor (notificação, antifraude, fidelidade) recebe pela sua **subscription**, com **filtros** avaliados pelo broker (SQL filter e correlation filter). O consumo é feito com `ServiceBusProcessor` em **peek-lock**, liquidando cada mensagem do jeito certo (complete, abandon com `DeliveryCount`, dead-letter com motivo), a **DLQ** é inspecionada e reenviada, eventos do mesmo pedido são ordenados com **sessions** e o broker cuida de **duplicatas** e **mensagens agendadas**.

Tudo roda contra o **emulador oficial do Azure Service Bus** em Docker, via `Testcontainers.ServiceBus` (o emulador precisa de um SQL Server: a fixture sobe `mssql/server:2022-latest` na mesma rede). A topologia (filas, tópico, subscriptions, regras) está em `src/F6M02.ServiceBus/Topologia/Config.json`, montado no container.

```bash
dotnet test labs/F6-M02-azure-service-bus
# ou só o projeto de testes:
dotnet test labs/F6-M02-azure-service-bus/tests/F6M02.ServiceBus.Tests
# um passo de cada vez:
dotnet test labs/F6-M02-azure-service-bus/tests/F6M02.ServiceBus.Tests --filter "FullyQualifiedName~Passo3"
```

Pré-requisito: Docker rodando e as imagens `mcr.microsoft.com/azure-messaging/servicebus-emulator:latest` e `mcr.microsoft.com/mssql/server:2022-latest` (a primeira execução baixa se faltar). O container sobe uma vez por execução (~13–16 s); a suíte completa leva ~40 s.

No início, os 20 testes falham. Ordem sugerida:

1. `Publicacao/MensagensDePedido.cs` — `MessageId`, `CorrelationId`, `Subject`, `ContentType`, `ApplicationProperties` e leitura do corpo (`Passo1`).
2. `Conexao/FabricaDeClienteServiceBus.cs` — connection string (emulador) × namespace + `TokenCredential` (Managed Identity) (`Passo2`).
3. `Publicacao/PublicadorDePedidos.cs` + **regras em `Topologia/Config.json`** (SQL filter `antifraude`, correlation filter `fidelidade`) (`Passo3`).
4. `Consumo/ConsumidorDePedidos.cs` — processor em peek-lock, complete/abandon/dead-letter, `MaxConcurrentCalls` (`Passo4`).
5. `Consumo/LeitorDeDeadLetter.cs` — inspecionar e reenviar a DLQ (`Passo5`).
6. `Sessoes/EventosDoPedido.cs` — `SessionId`, `ServiceBusSessionProcessor`, session state (`Passo6`).
7. `Pagamentos/` — detecção de duplicatas e `ScheduleMessageAsync` (`Passo7`).

Já vêm prontos: contratos, nomes das entidades, `OrigemDasMensagens`, `ResultadoDoProcessamento` e toda a infra de teste (`tests/.../Infra`). **Não altere os testes.**

Limitações do emulador encontradas (documentadas no Lab): cancelar mensagem agendada não impede a entrega; `DeliveryCount` aparece 0 no peek da DLQ; sem Entra ID/Managed Identity, sem partições, sem JMS, sem AMQP sobre WebSockets; máximo de 10 conexões e TTL máximo de 1 h.
