# F6-M05 — Retries, DLQ e Sagas

**Módulo:** Retries, DLQ e Sagas (Fase 6)
**Tempo:** 3–4 horas

Duas metades do mesmo problema: **falhas parciais** em fluxos assíncronos.

1. **Retry e DLQ no consumidor (RabbitMQ)**: classificação de erro transitório × permanente, retry imediato limitado, retry atrasado com **filas de espera com TTL + dead-letter exchange** de volta para a fila principal, dead-letter queue **com motivo** nos headers e uma ferramenta de **reprocessamento da DLQ** (listar sem consumir, mover com filtro, preservando o `MessageId`).
2. **Saga orquestrada de pedido**: `PedidoCriado` → reservar estoque → autorizar pagamento → confirmar pedido; pagamento recusado ou sem resposta no prazo → **compensação** (liberar estoque) → cancelar. Estado (`SagaPedido`) no **SQL Server** com **concorrência otimista** (`WHERE Versao = @Versao`), idempotência por `MessageId`, mensagens fora de ordem e duplicadas, timeout com `FakeTimeProvider` e um teste ponta a ponta com participantes falando AMQP de verdade.
3. **Coreografia** (contraste): o mesmo fluxo com dois serviços reagindo a eventos num exchange topic, sem coordenador.

Infra de teste: **Testcontainers** sobe um `rabbitmq:4.1-management` e um `mcr.microsoft.com/mssql/server:2022-latest` uma vez por execução (em paralelo, portas aleatórias). Nada de `Task.Delay` fixo para esperar mensagem: os testes usam `Esperar.Eventualmente(...)` (polling curto com prazo).

```bash
dotnet test labs/F6-M05-retries-dlq-sagas
# ou só o projeto de testes:
dotnet test labs/F6-M05-retries-dlq-sagas/tests/F6M05.Tests
# um passo de cada vez:
dotnet test labs/F6-M05-retries-dlq-sagas/tests/F6M05.Tests --filter "FullyQualifiedName~Parte1_Retry"
```

Pré-requisito: Docker rodando. No início, os **48 casos de teste falham** (`NotImplementedException("TODO: ...")`). Ordem sugerida:

| Passo | Arquivo | Testes |
|---|---|---|
| 1 | `Retry/ClassificadorDeErros.cs` | `ClassificadorDeErrosTests` |
| 2 | `Retry/TopologiaDeRetry.cs` | `TopologiaDeRetryTests` |
| 3 | `Retry/ConsumidorComRetry.cs` (`ProcessarEntregaAsync`, `EnviarParaDlqAsync`) | `ConsumidorComRetryTests` |
| 4 | `Retry/ReprocessadorDeDlq.cs` | `ReprocessadorDeDlqTests` |
| 5 | `Saga/SagaPedido.cs` (máquina de estados pura) | `SagaPedidoTests` |
| 6 | `Saga/RepositorioDeSagasSql.cs` (otimista) | `RepositorioDeSagasTests` |
| 7 | `Saga/OrquestradorSagaPedido.cs` | `OrquestradorSagaPedidoTests` |
| 8 | `Saga/VerificadorDePrazos.cs` | `OrquestradorSagaPedidoTests` (timeout, corrida) |
| 9 | — (só roda) | `SagaPontaAPontaTests` |
| 10 | `Coreografia/ServicosCoreografados.cs` | `CoreografiaTests` |

Já vêm prontos: `Mensageria/` (canal com publisher confirms, `MensagemRecebida`, headers, JSON), `PoliticaDeRetry`, mensagens e comandos da saga, enums de estado, schema SQL (`Sql/Esquema.sql`), `HospedeiroDaSaga`, base da coreografia, logs (`Log.cs`) e toda a infra de teste (`tests/F6M05.Tests/Infra/`). **Não altere os testes.**

A suíte roda em ~11 s (incluindo subir os dois containers). O gabarito está na branch `solucoes`.
