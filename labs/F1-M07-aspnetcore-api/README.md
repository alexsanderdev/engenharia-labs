# F1-M07 — ASP.NET Core e APIs

**Módulo:** ASP.NET Core e APIs (Fase 1)
**Tempo:** 2 horas

CRUD de produtos em memória com Minimal APIs: route group, `TypedResults`, validação via endpoint filter, middleware de correlation id e testes de integração com `WebApplicationFactory`. É a base do OrderFlow v0.1.

```bash
dotnet test labs/F1-M07-aspnetcore-api/tests/F1M07.Api.Tests
dotnet run --project labs/F1-M07-aspnetcore-api/src/F1M07.Api   # http://localhost:5107
```

1. `Products/ProductValidator.cs` — regras de validação (testes `ProductValidatorTests`).
2. `Products/ProductEndpoints.cs` — handlers com `TypedResults` e o mapeamento do grupo `/products`.
3. `Products/ValidationFilter.cs` — endpoint filter que devolve `ValidationProblem` (400).
4. `Infrastructure/CorrelationIdMiddleware.cs` — header `X-Correlation-Id` ecoado em toda resposta.
5. Explore a API com `src/F1M07.Api/F1M07.Api.http`.

O repositório em memória e o `Program.cs` já vêm prontos (leia a ordem do pipeline). **Não altere os testes.**
OpenAPI nativo habilitado: com a API rodando em Development, o documento fica em `GET /openapi/v1.json`.
