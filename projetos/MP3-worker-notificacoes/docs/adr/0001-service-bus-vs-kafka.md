# ADR 0001 — Azure Service Bus (e não Kafka) para as notificações de pedido

- **Status:** aceito (gabarito do MP3 — o seu ADR pode chegar a outra conclusão, desde que justificada)
- **Data:** 2026-10
- **Contexto do sistema:** OrderFlow v1.2, Worker de Notificações (MP3)

## Contexto

Quando um pedido é criado, a API publica `PedidoCriado` (via Outbox). O worker precisa notificar o cliente
por e-mail/SMS. Requisitos que pesam na escolha do broker:

1. **Uma notificação por evento**, mesmo com reentrega (at-least-once) e com várias instâncias do worker.
2. **Retry com backoff** em falha transitória do provedor e **quarentena** (DLQ) para falha permanente, com reprocessamento manual.
3. **Escala modesta:** dezenas a poucas centenas de mensagens por minuto em pico; crescimento previsto de 10×, não de 1000×.
4. **Operação enxuta:** time pequeno, já no Azure (Container Apps, Azure SQL, Key Vault, Managed Identity).
5. **Ordem** entre eventos de pedidos diferentes **não importa** para notificação.
6. Não há necessidade de **reler o histórico** de eventos para este consumidor.

## Opções consideradas

| Critério | Azure Service Bus (fila/tópico) | Kafka (Event Hubs com API Kafka, Confluent ou self-hosted) |
|---|---|---|
| Modelo | Fila com lock por mensagem; competing consumers | Log particionado com offset por grupo de consumidores |
| Settle por mensagem (complete/abandon/dead-letter) | Nativo | Não existe: commit de offset é por partição; "pular" uma mensagem exige tópico de retry/DLQ próprio |
| DLQ | Nativa, com motivo e descrição, `MaxDeliveryCount` | Convenção da aplicação (tópico `*.dlq`) |
| Retry com atraso | Mensagem agendada (`ScheduledEnqueueTime`) | Tópicos de retry por atraso ou pausa da partição |
| Paralelismo | Qualquer número de consumidores na mesma fila | Limitado ao nº de partições por grupo |
| Ordem | FIFO só com sessions | Por partição (chave) |
| Replay / reprocessar histórico | Não (mensagem completada some) | Sim (retenção + reset de offset) |
| Throughput | Alto o bastante (Standard/Premium) | Muito alto (o ponto forte) |
| Operação no Azure | PaaS, Managed Identity, emulador local oficial | Event Hubs (PaaS) ou cluster a operar; mais conceitos (partições, rebalance, lag) |
| Custo para este volume | Baixo (Standard) | Event Hubs Standard/Premium; self-hosted custa gente |

## Decisão

Usar **Azure Service Bus**: a API publica no tópico `pedidos`; a subscription `notificacoes` encaminha (`ForwardTo`)
para a fila `notificacoes-pedidos`, consumida por este worker com `ServiceBusProcessor` em peek-lock, sem
auto-complete. Idempotência por **inbox no SQL Server** (chave `Consumidor + EventoId`), retry por **reagendamento**
com backoff exponencial + jitter, DLQ nativa para falha permanente, contrato inválido e tentativas esgotadas.

## Consequências

**Positivas**

- Os requisitos 1 e 2 saem quase "de fábrica": lock por mensagem, DLQ com motivo, `MaxDeliveryCount`, mensagem agendada.
- Escala horizontal trivial (mais réplicas na mesma fila), sem pensar em partições.
- Desenvolvimento e testes com o **emulador oficial** (Testcontainers), o mesmo SDK de produção.
- Managed Identity e RBAC do Azure, sem segredo de conexão em produção (v1.3).

**Negativas / riscos aceitos**

- **Sem replay**: se amanhã precisarmos reconstruir uma projeção a partir dos `PedidoCriado` antigos, o Service Bus
  não ajuda. Mitigação: a Outbox guarda os eventos publicados; um tópico Kafka/Event Hubs pode ser adicionado depois
  **para analytics**, sem mexer neste worker.
- Retry por reagendamento cria uma cópia da mensagem: entre "agendar" e "completar" pode sobrar uma duplicata —
  absorvida pela inbox (por isso a ordem é agendar → completar).
- O emulador não cobre tudo (ex.: management por SDK, alguns limites de cota); a topologia vive em `Config.json`
  e em Bicep (v1.3), não em código.
- Dependência de um serviço Azure (lock-in moderado): o worker isola o SDK em `Mensageria/`; o caso de uso
  (`ProcessadorDePedidoCriado`) não conhece o broker.

## Quando revisitar

- Volume > ~1–2 mil mensagens/s sustentadas, necessidade de **replay** para vários consumidores, ou **ordem por pedido**
  com alto paralelismo → reavaliar Kafka/Event Hubs (ver o módulo 6.03 — Kafka).
- Notificações precisarem de ordem estrita por cliente → Service Bus **sessions** (SessionId = ClienteId) antes de trocar de broker.
