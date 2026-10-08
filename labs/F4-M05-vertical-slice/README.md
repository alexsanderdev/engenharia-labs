# F4-M05 — Vertical Slice

**Módulo:** Vertical Slice (Fase 4)
**Tempo:** 2–3 horas

A API de pedidos do OrderFlow está organizada em **camadas horizontais**: `Controllers/`, `Services/`, `Repositories/`, `DTOs/`, `Models/`. Para entender "Cancelar pedido" você abre 6 arquivos em 5 pastas; o `PedidoService` faz-tudo cresce a cada feature; validação, cálculo de total e regras de status estão duplicados.

Seu trabalho: migrar para **fatias verticais** em `Features/Pedidos/` — um arquivo por caso de uso com request, validator (FluentValidation), handler e endpoint juntos — sem mudar NENHUM comportamento HTTP.

```bash
dotnet test labs/F4-M05-vertical-slice
```

| Tipo | Classe | Início | Regra |
|---|---|---|---|
| Caracterização | `ComportamentoPedidosApiTests` (15) | verde | **nunca** pode ficar vermelho, em nenhum passo |
| Design | `DesignVerticalSliceTests` (9) | vermelho | fica verde quando a estrutura vira fatias |

Os testes de comportamento usam `WebApplicationFactory<Program>` e um armazenamento **em memória próprio** (sem EF InMemory, sem SQLite, sem Docker). Os de design usam reflexão, NetArchTest e Mono.Cecil (lendo o IL) para verificar: sem pastas por tipo técnico, sem `*Service`/`*Repository`/`*Controller`, cada fatia com `Command`/`Query`, `Validator`, `Handler` e `Endpoint`, commands × queries separados (CQRS), pipeline de validação envolvendo os commands, fatias que não dependem umas das outras e regras de status no domínio.

A plataforma das fatias já está pronta em `Comum/` (sem MediatR): `ICommandHandler<,>`, `IQueryHandler<,>`, `IEndpoint`, o decorator de validação, o registro por reflexão (`AddFeatures`/`MapFeatures`) e o tratamento de erros (`TratamentoDeErros` → ProblemDetails). Leia antes de começar.

## Passos

1. Leia o código em camadas e marque as duplicações.
2. Domínio rico: `Models/` vira `Dominio/Pedido.cs` com `Criar`, `Confirmar()`, `Cancelar()` e `Status` com setter privado; o `PedidoService` passa a chamar o domínio.
3. Crie `Infraestrutura/BancoEmMemoria.cs` (catálogo semente com os MESMOS ids), registre-o e apague `Repositories/`.
4. Migre uma fatia por vez, apagando a action do controller e o método do service no mesmo passo: `ObterPedido` → `ListarPedidos` → `CriarPedido` → `ConfirmarPedido` → `CancelarPedido`.
5. Apague `Controllers/`, `Services/`, `DTOs/` e as linhas de controllers do `Program.cs`.

Rode `dotnet test` ao fim de **cada** passo: os 15 testes de caracterização ficam verdes o tempo todo.

O lab termina com os **24** testes verdes, sem alterar os testes. Gabarito na branch `solucoes`.
