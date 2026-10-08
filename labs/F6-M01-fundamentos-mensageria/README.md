# F6-M01 — Fundamentos de Mensageria

**Módulo:** Fundamentos de Mensageria (Fase 6)
**Tempo:** 2–3 horas
**Requer Docker** (Testcontainers sobe um RabbitMQ 4.1; imagem `rabbitmq:4.1-management`).

A mensageria do OrderFlow sobre o **RabbitMQ.Client 7** (API 100% assíncrona), sem framework por cima: catálogo de contratos que separa **comando** (`ReservarEstoque`, um destinatário) de **evento** (`PedidoCriado`, `PedidoCancelado`, N interessados), envelope com `MessageId`, `CorrelationId`, tipo e versão do contrato, topologia durável (exchange `direct` de comandos, exchange `topic` de eventos com bindings `pedido.criado` e `pedido.*`), publisher confirms, mensagens persistentes, consumo com **ack manual** e **prefetch**, competing consumers e redelivery quando um consumidor cai antes do ack (at-least-once).

```bash
docker pull rabbitmq:4.1-management   # uma vez
dotnet test labs/F6-M01-fundamentos-mensageria
# ou só o projeto de testes:
dotnet test labs/F6-M01-fundamentos-mensageria/tests/F6M01.Mensageria.Tests
```

Com a imagem já baixada, a suíte roda em ~10 s (o container sobe uma vez por execução; os testes com broker rodam em série e resetam filas/exchanges antes de cada teste). Nenhum teste usa `Task.Delay` fixo para "esperar a mensagem": `Infra/Eventualmente` faz polling com timeout e os consumidores sinalizam com `TaskCompletionSource`.

## O que você completa (nesta ordem)

1. `Contratos/CatalogoDeContratos.cs` — os 3 contratos (`ContratosTests.Catalogo_*`).
2. `Contratos/Envelope.cs` — `Criar` e `LerCorpo` (tolerant reader + versão) (`ContratosTests`).
3. `Contratos/MapeamentoAmqp.cs` — envelope ↔ `BasicProperties` (`MapeamentoAmqpTests`).
4. `Topologia/TopologiaOrderFlow.cs` — `DeclararAsync` (`TopologiaTests`).
5. `Publicacao/PublicadorDeMensagens.cs` — publisher confirms, `mandatory` para comandos (`ComandosEEventosTests`).
6. `Consumo/ConsumidorDeFila.cs` — `IniciarAsync` (QoS + `autoAck: false`).
7. `Consumo/ConsumidorDeFila.cs` — `AoReceberAsync` (ack/nack, segunda chance, mensagem venenosa) (`ConsumoTests`).

No início, os **31 casos de teste falham**. O lab termina quando todos ficam verdes **sem alterar os testes**. Rode a suíte algumas vezes seguidas: nenhum teste pode ser intermitente.

Já vêm prontos: as mensagens (`Mensagens.cs`), as constantes da topologia, os `DisposeAsync` e toda a infra de teste (`tests/.../Infra/`: container, reset, `Eventualmente`).

Roteiro completo e dicas: nota "Fundamentos de Mensageria - Lab" no vault do curso. Gabarito na branch `solucoes`.
