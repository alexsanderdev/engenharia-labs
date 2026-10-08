# F4-M04 — CQRS

**Módulo:** CQRS (Fase 4)
**Tempo:** 2 horas

Commands e queries do módulo Pedidos do OrderFlow com um **dispatcher próprio** (sem MediatR): `ICommand<T>`/`IQuery<T>`, handlers registrados por varredura de assembly, **decorators** de logging, validação (FluentValidation) e unidade de trabalho (só commands), e um **read model** separado, atualizado por uma projeção a partir de eventos de domínio em processo. Tudo em memória: sem banco, sem Docker.

```bash
dotnet test labs/F4-M04-cqrs
# ou só o projeto de testes:
dotnet test labs/F4-M04-cqrs/tests/F4M04.Cqrs.Tests
```

No início, os 22 testes falham. Ordem sugerida:

1. `Dispatching/Dispatcher.cs` — roteamento pelo tipo concreto da mensagem (`DispatcherTests`).
2. `Dispatching/CqrsServiceCollectionExtensions.cs` — `AddCqrs`: varredura, decorators, duplicados (`RegistroTests`).
3. `Pipeline/LoggingDecorators.cs`, `ValidationDecorators.cs`, `UnitOfWorkCommandDecorator.cs` (`PipelineTests`).
4. `Pedidos/Escrita/*Handler` — `CriarPedido` e `ConfirmarPedido` (`PedidosTests`).
5. `Pedidos/Leitura/` — projeção, `ObterPedido`, `ListarPedidosDoCliente` e o validator da query (`ProjecaoTests`, `LeituraTests`).

Já vêm prontos: abstrações (`Abstractions/`), domínio (`Pedido`, eventos), infraestrutura em memória (`BancoDeEscrita`, `BancoDeLeitura`, repositório, `UnitOfWorkEmMemoria`, `DomainEventPublisher`) e o `CriarPedidoValidator` (modelo). Os testes usam tipos próprios (`tests/.../Infra/TiposDeTeste.cs`) que escrevem no log para provar a ordem do pipeline. **Não altere os testes.**
