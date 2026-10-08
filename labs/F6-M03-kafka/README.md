# F6-M03 — Kafka

**Módulo:** Kafka (Fase 6)
**Tempo:** 2–3 horas · **Requer Docker** (imagem `confluentinc/cp-kafka:7.9.0`)

Eventos `PedidoCriado`/`PedidoConfirmado` do OrderFlow num broker Kafka real (Testcontainers, modo KRaft): contrato JSON versionado em headers, tópicos com N partições via `AdminClient`, producer idempotente (`EnableIdempotence`, `Acks.All`, chave = `PedidoId`), ordem por chave, consumer groups e rebalance, commit manual (at-least-once), `AutoOffsetReset` Earliest × Latest, consumidor idempotente e retry/DLQ feitos pelo consumidor com headers de diagnóstico.

```bash
docker pull confluentinc/cp-kafka:7.9.0   # uma vez
dotnet test labs/F6-M03-kafka
# ou só o projeto de testes:
dotnet test labs/F6-M03-kafka/tests/F6M03.Kafka.Tests
```

No início, os **24 testes falham**. Ordem sugerida:

1. `Contratos/SerializadorDeEventos.cs` — chave, JSON e headers com versão (`ContratoTests`, sem broker).
2. `Topicos/AdministradorDeTopicos.cs` — criar tópico/topologia e contar partições (`TopicoEPublicacaoTests`).
3. `Producao/PublicadorDePedidos.cs` — config segura e `ProduceAsync` (`TopicoEPublicacaoTests`).
4. `Consumo/ConsumidorDePedidos.cs` — `CriarConfig` e `ProcessarProximoAsync` com commit manual (`ConsumoTests`).
5. Mesma classe — `AoAtribuir`/`AoRevogar` (rebalance) (`ConsumoTests`).
6. `Consumo/Manipuladores.cs` — `ManipuladorIdempotente` (`ManipuladorIdempotenteTests` e o primeiro de `RetryEDlqTests`).
7. `Consumo/EncaminhadorDeFalhas.cs` + catch no consumidor — retry/DLQ (`RetryEDlqTests`).

**Infra pronta (leia, não altere):** `tests/.../Infra/KafkaFixture.cs` sobe UM broker por execução (porta aleatória, `KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS=0`); cada teste usa tópicos e grupos com nome único. `Infra/Apoio.cs` tem `Eventualmente` (polling com timeout — nada de `Task.Delay` "torcendo"), `LerDoInicio` (lê um tópico sem grupo) e o `ManipuladorDeTeste`, que falha de propósito. A suíte roda em ~35 s com a imagem baixada.

Prontos em `src`: `Contratos/Eventos.cs`, `Contratos/Cabecalhos.cs`, `RegistroEmMemoria` e o esqueleto das classes. **Não altere os testes.**
