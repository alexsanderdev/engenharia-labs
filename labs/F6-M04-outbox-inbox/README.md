# F6-M04 — Outbox, Inbox e Idempotência

**Módulo:** Outbox, Inbox e Idempotência (Fase 6)
**Tempo:** 3–4 horas
**Requer Docker** (Testcontainers sobe um SQL Server 2022 e um RabbitMQ 4.1; imagens `mcr.microsoft.com/mssql/server:2022-latest` e `rabbitmq:4.1-management`).

O OrderFlow cria e confirma pedidos e precisa avisar o mundo (`pedido.criado`, `pedido.confirmado`). A versão inicial faz **dual write**: salva no banco e publica direto no broker. Você vai trocar isso por **Transactional Outbox** (evento gravado na mesma transação do pedido + processor que publica com publisher confirms, em várias instâncias, com retry, backoff, ordem por pedido e desistência de mensagens envenenadas) e escrever, do outro lado, um consumidor **idempotente com Inbox**.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez
docker pull rabbitmq:4.1-management                       # uma vez
dotnet test labs/F6-M04-outbox-inbox
```

Com as imagens baixadas, a suíte roda em ~15 s (os containers sobem uma vez, em paralelo).

## Projetos

- `src/F6M04.Pedidos` — agregado `Pedido`, `PedidosDbContext` (Pedidos + `OutboxMessages`), `ServicoDePedidos`, `OutboxInterceptor`, `OutboxProcessor`, `PublicadorRabbitMq`. Em `Legado/`, a versão dual write ingênua (não corrija: é material dos testes de demonstração).
- `src/F6M04.Notificacoes` — outro serviço, outro banco: `NotificacoesDbContext` (Notificacoes + `InboxMessages`), `ConsumidorDeNotificacoes` e `WorkerDeNotificacoes` (RabbitMQ, ack manual, DLQ). Não referencia Pedidos: o contrato é a mensagem.
- `tests/F6M04.Tests` — infraestrutura PRONTA (containers, topologia exclusiva por teste, relógio falso, dublês que falham, ponto de parada) e os testes.

## O que você completa (nesta ordem)

1. `Outbox/OutboxInterceptor.cs` e `Aplicacao/ServicoDePedidos.cs` — evento na Outbox na mesma transação; o caso de uso para de publicar.
2. `Mensageria/PublicadorRabbitMq.cs` — publicação com publisher confirms e `mandatory`.
3. `Outbox/OutboxProcessor.ProcessarLoteAsync` — reserva com `UPDLOCK, READPAST, ROWLOCK`, publicação, marcação, commit.
4. Tentativas, erro, backoff e mensagem envenenada (mesmo método).
5. Ordem por pedido (`VersaoDoAgregado`) e duas instâncias concorrentes.
6. `ExecuteAsync` com `PeriodicTimer` + `TimeProvider` e `LimparProcessadasAsync`.
7. `Inbox/ConsumidorDeNotificacoes.ProcessarAsync` — Inbox + efeito no mesmo `SaveChanges`, duplicata concorrente.
8. `Mensageria/WorkerDeNotificacoes.TratarEntregaAsync` — ack depois do commit, nack para a DLQ.

**Não altere os testes.** No início, 20 testes falham e os 2 de demonstração (`Demonstracoes/DualWriteTests`) passam; o lab termina quando os 22 ficam verdes. Rode a suíte algumas vezes seguidas: nenhum teste pode ser intermitente.

Roteiro completo e dicas: nota "Outbox, Inbox e Idempotência - Lab" no vault do curso. Gabarito na branch `solucoes`.
