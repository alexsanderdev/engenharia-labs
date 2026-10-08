# F2-M07 — Testes de Integração

**Módulo:** Testes de Integração (Fase 2)
**Tempo:** 2 horas
**Requer Docker** (Docker Desktop no Windows/macOS; no GitHub Actions, `ubuntu-latest` já tem).

API de pedidos do OrderFlow (Minimal API + EF Core + SQL Server) testada contra um **SQL Server de verdade** em container: Testcontainers sobe o banco uma vez, `WebApplicationFactory<Program>` aponta a API para ele, Respawn limpa os dados antes de cada teste e um handler de autenticação fake define quem é o usuário. Cada teste verifica **status code, payload e efeito persistido**.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F2-M07-testes-integracao
```

Com a imagem já baixada, a suíte inteira roda em ~10–15 s (o container sobe uma vez só).

O que você completa (nesta ordem):

1. `tests/.../Infra/ApiFixture.cs` — `SubirContainerAsync` (Testcontainers, imagem `mssql/server:2022-latest`).
2. `tests/.../Infra/PedidosApiFactory.cs` (`ApontarParaOContainer`) + `ApiFixture.ComBancoAsync` + `CriarSchemaAsync` (`EnsureCreated`).
3. `ApiFixture.PrepararRespawnAsync` + `ResetarBancoAsync` (Respawn).
4. `tests/.../Infra/TestAuthHandler.cs` + `PedidosApiFactory.TrocarAutenticacao` (autenticação fake).
5. `src/.../Infrastructure/ErrosDeBanco.cs` — traduzir violação de índice único em 409.
6. `src/.../Pedidos/PedidoEndpoints.cs` — `Criar` e `Cancelar`.

Já vem pronto: o resto da API (`Program.cs`, `PedidosDbContext`, entidades, validadores, leitura de pedidos, produtos) e a base dos testes `IntegracaoTestBase` (reset antes de cada teste, `CriarCliente`, `SemearProdutoAsync`, `SemearPedidoAsync`).

**Não altere os testes** (`*Tests.cs`). No início os 19 falham; o lab termina quando todos ficam verdes.
