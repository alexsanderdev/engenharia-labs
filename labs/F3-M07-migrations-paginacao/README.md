# F3-M07 — Migrations, Concorrência e Paginação

**Módulo:** Migrations, Concorrência e Paginação (Fase 3)
**Tempo:** 2 a 3 horas
**Requer Docker** (imagem `mcr.microsoft.com/mssql/server:2022-latest`) e a ferramenta `dotnet ef` do manifesto do repo.

Três problemas de dados que todo sistema em produção enfrenta, no domínio do OrderFlow:

- **Migrations EF Core** — você cria a migration inicial e uma segunda que adiciona uma coluna `NOT NULL` numa tabela **com dados** (expand → backfill → NOT NULL + DEFAULT), e gera o script idempotente. Os testes aplicam as SUAS migrations em bancos vazios e conferem schema e dados.
- **Concorrência otimista** com `rowversion`: devolver conflito (409) ou recarregar e reaplicar.
- **Paginação offset × keyset** com cursor opaco (Base64Url de `CriadoEm` + `Id`), provando que keyset não pula nem repete itens e lê muito menos páginas em páginas profundas.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez
dotnet tool restore                                       # instala o dotnet ef do manifesto
dotnet test labs/F3-M07-migrations-paginacao
```

Comandos `dotnet ef` do lab (rode na raiz do repo; o projeto já tem `Microsoft.EntityFrameworkCore.Design` e uma `IDesignTimeDbContextFactory`):

```bash
dotnet build labs/F3-M07-migrations-paginacao/src/F3M07.Dados
dotnet ef migrations add Inicial --project labs/F3-M07-migrations-paginacao/src/F3M07.Dados
dotnet ef migrations add AdicionaCanalAoPedido --project labs/F3-M07-migrations-paginacao/src/F3M07.Dados
dotnet ef migrations script --idempotent --project labs/F3-M07-migrations-paginacao/src/F3M07.Dados -o labs/F3-M07-migrations-paginacao/src/F3M07.Dados/Scripts/migrations-idempotente.sql
```

O que você faz (nesta ordem):

1. `Persistencia/LojaDbContext.cs` — `Versao` como rowversion e o índice `IX_Pedidos_CriadoEm_Id` (**antes** da primeira migration).
2. `dotnet ef migrations add Inicial`.
3. Troque o `Ignore(Canal)` pela configuração da coluna, gere `AdicionaCanalAoPedido` e **edite** a migration: pedidos antigos recebem `'Legado'`, novos inserts sem canal recebem o default `'Web'`.
4. Gere o script idempotente (e gere de novo sempre que mudar uma migration).
5. `Concorrencia/AlteracaoDePedidos.cs` — `CancelarAsync` (conflito → 409) e `AplicarDescontoAsync` (recarregar e reaplicar).
6. `Paginacao/CursorDePaginacao.cs` e `Paginacao/PaginacaoDePedidos.cs` — offset, keyset e cursor.

Já vem pronto: entidades, `LojaDbContextFactory`, e toda a infraestrutura de teste (container único, bancos descartáveis por teste de migration, Respawn no banco principal, interceptor que simula uma escrita concorrente de forma determinística, medição de leituras lógicas).

**Não altere os testes.** No início os 28 casos falham; o lab termina quando todos ficam verdes (a suíte leva ~15 s com a imagem já baixada).
