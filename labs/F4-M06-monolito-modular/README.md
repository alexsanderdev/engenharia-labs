# F4-M06 — Monólito Modular

**Módulo:** Monólito Modular (Fase 4)
**Tempo:** 3 horas
**Requer Docker** (SQL Server 2022 em container via Testcontainers).

O OrderFlow como **monólito modular**: um Host ASP.NET Core que compõe três módulos — **Catálogo**, **Pedidos** e **Clientes** — cada um com projeto de implementação (tudo `internal`), projeto `.Contracts` (o que os outros podem usar), `DbContext` próprio e **schema próprio** no mesmo banco. O código inicial **funciona**, mas as fronteiras estão furadas e faltam peças.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F4-M06-monolito-modular
```

Com a imagem já baixada, a suíte roda em ~20 s (um container para os testes de integração; o Respawn limpa os schemas antes de cada teste).

```
src/F4M06.Host                       composition root (Program.cs): AddInProcessEventBus + Register/MapEndpoints de cada IModule
src/F4M06.Shared                     IModule, IEventBus (em processo), IIntegrationEventHandler<T>, inicializador de schema
src/Modules/Catalogo/F4M06.Catalogo(.Contracts)   ICatalogoApi + ProdutoResumo
src/Modules/Pedidos/F4M06.Pedidos(.Contracts)     evento PedidoConfirmado
src/Modules/Clientes/F4M06.Clientes(.Contracts)   IClientesApi (já usado do jeito certo por Pedidos: use como modelo)
tests/F4M06.Arquitetura.Tests        NetArchTest + reflexão + leitura dos .csproj (fronteiras)
tests/F4M06.Integracao.Tests         WebApplicationFactory + Testcontainers (comportamento, contrato, eventos, schemas)
```

## Estado inicial

- **19 casos de arquitetura**: 8 falham (Pedidos usa `CatalogoDbContext`/`Produto`; classes públicas que deveriam ser `internal`; DbContexts públicos; `CatalogoApi` pública; `F4M06.Pedidos.csproj` referencia a implementação do Catálogo).
- **12 testes de integração**: 5 falham (contrato `ICatalogoApi` não registrado; tabelas de Pedidos no `dbo`; `PedidoConfirmado` não é publicado nem assinado).
- `ComportamentoPedidosTests` (6 testes de **caracterização**) passam desde o início e **nunca podem ficar vermelhos** no meio da refatoração.

## O que fazer (detalhes na nota "Monólito Modular - Lab")

1. Rode tudo e leia as mensagens: cada regra lista os tipos culpados.
2. `Catalogo/Fachada/CatalogoApi.cs`: implemente o contrato (consulta em lote, projeção para `ProdutoResumo`) e registre `ICatalogoApi` no `CatalogoModule`.
3. Pedidos passa a usar `ICatalogoApi`; troque no `F4M06.Pedidos.csproj` a referência `F4M06.Catalogo` por `F4M06.Catalogo.Contracts`.
4. Torne `internal` tudo o que não é a classe do módulo (entidades, DbContexts, endpoints, DTOs, `CatalogoApi`).
5. Pedidos publica `PedidoConfirmado` (depois do commit) via `IEventBus`; Clientes implementa `PedidoConfirmadoHandler` (idempotente) e assina com `AddIntegrationEventHandler<,>`.
6. Pedidos grava no próprio schema (`pedidos`).

**Não altere os testes.** O lab termina quando tudo está verde. Gabarito na branch `solucoes` (inclui o `F4M06.Pedidos.csproj` corrigido).
