# F3-M04 — EF Core e Persistência

**Módulo:** EF Core e Persistência (Fase 3)
**Tempo:** 2 horas
**Requer Docker** (SQL Server 2022 em container via Testcontainers).

Você escreve a persistência do OrderFlow com EF Core: o `OrderFlowDbContext`, as configurações com Fluent API (`IEntityTypeConfiguration<T>`) e um `PedidoService` com consultas controladas. Os testes verificam três coisas: o **modelo** do EF (metadata), o **schema** que ele cria no SQL Server real e o **comportamento** das consultas (Include, projeção para DTO, AsNoTracking, change tracker).

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F3-M04-efcore
```

Com a imagem já baixada, a suíte roda em ~10–15 s: o container sobe uma vez, o schema é criado uma vez a partir do **seu** modelo (`EnsureCreated`) e cada teste roda dentro de uma transação desfeita no fim.

O que você completa (nesta ordem):

1. `src/.../Persistencia/OrderFlowDbContext.cs` — `ApplyConfigurationsFromAssembly`.
2. `src/.../Persistencia/Configuracoes/*.cs` — tabelas, chaves, tamanhos, precisão de `decimal`, `Status` como texto.
3. Mesmas classes — índices únicos (SKU, e-mail), FKs com `Restrict`, Pedido → Itens um-para-muitos com cascade.
4. `ProdutoConfiguration` — seed do catálogo inicial com `HasData`.
5. (Nenhum código novo) — rode `SchemaTests` e confira o que virou no SQL Server.
6. `src/.../Pedidos/PedidoService.cs` — leituras: `AsNoTracking`, `Include`, projeção com `Select`.
7. `PedidoService.CriarAsync` — escrita com total calculado no servidor e `TimeProvider`.
8. `PedidoService.ConfirmarAsync` — change tracker gerando só o UPDATE necessário.
9. `src/.../PersistenciaServiceCollectionExtensions.cs` — registro scoped do DbContext.

Já vem pronto: entidades do domínio (`Dominio/`), `CatalogoInicial`, contratos (DTOs) e toda a infraestrutura de teste (`tests/.../Infra/`: container, transação por teste, helpers de seed).

**Não altere os testes.** No início os 20 falham; o lab termina quando todos ficam verdes. Gabarito na branch `solucoes`.
