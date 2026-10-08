# F4-M01 — Clean Architecture

**Módulo:** Clean Architecture (Fase 4)
**Tempo:** 2–3 horas

Um OrderFlow em 4 projetos (`Domain`, `Application`, `Infrastructure`, `Api`) que **parece** Clean Architecture, mas não é: o caso de uso "Criar pedido" está inteiro no `Program.cs` da Api (regra + EF Core direto + preço vindo do cliente + `DateTime.UtcNow`), o domínio usa atributo do EF Core, a Application referencia a Infrastructure e as entidades têm setter público.

```bash
dotnet test labs/F4-M01-clean-architecture
```

| Projeto de teste | O que verifica | Início |
|---|---|---|
| `F4M01.Arquitetura.Tests` | Regra da dependência (NetArchTest), portas × adaptadores, composition root da Api, modelo do EF Core | 11 de 17 vermelhos |
| `F4M01.Application.Tests` | Casos de uso `CriarPedidoHandler` e `ListarPedidosDoClienteHandler` com **fakes** das portas — sem banco, sem web | 11 de 12 vermelhos |

Nenhum teste abre conexão com SQL Server: a connection string do `appsettings.json` nunca é usada.

## Passos

1. Leia `Program.cs` da Api e liste as regras de negócio escondidas no endpoint.
2. **Domain:** implemente `Pedido.Criar`, troque setters por `private set`, `Itens` vira `IReadOnlyList` com campo `_itens`, remova `[Precision]` e a referência a EF Core do `F4M01.Domain.csproj`.
3. **Application + Infrastructure:** implemente `CriarPedidoHandler` (só portas), troque o `OrderFlowDbContext` do `ListarPedidosDoClienteHandler` por `IPedidoRepository`, **inverta** as referências de projeto (Application deixa de referenciar Infrastructure; Infrastructure passa a referenciar Application) e crie os adaptadores (`PedidoRepositoryEf`, `ProdutoRepositoryEf`, `RelogioDoSistema`) + `AddInfrastructure`.
4. **Api:** endpoint magro em `Endpoints/`, DTO de request sem preço, `IExceptionHandler` traduzindo `DomainException` → 422 e `ProdutoNaoEncontradoException` → 404.
5. **Composition root:** `Composicao/ComposicaoDaApi.cs` é o único lugar da Api que conhece a Infrastructure.

Você corrige o **código** (e os `.csproj`), nunca os testes. O lab termina com os **29** testes verdes.
Gabarito na branch `solucoes` (inclui os `.csproj` de Domain, Application e Infrastructure corrigidos).
