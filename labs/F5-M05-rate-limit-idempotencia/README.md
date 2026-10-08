# F5-M05 — Rate Limiting e Idempotência de API

**Módulo:** Rate Limiting e Idempotência de API (Fase 5)
**Tempo:** 2–3 horas

API de Catálogo e Pedidos do OrderFlow com (a) **rate limiting** do ASP.NET Core — janela fixa, janela deslizante, token bucket e concorrência, partição por cliente (API key) e por IP, 429 com `Retry-After` e ProblemDetails, uma política por endpoint; (b) **Idempotency-Key** no `POST /pedidos` — replay, 422 para corpo diferente, 409 para requisição concorrente, liberação em 5xx e expiração com `FakeTimeProvider`, atrás de uma interface pronta para Redis; (c) **output caching** no `GET /catalogo` com invalidação por tag.

```bash
dotnet test labs/F5-M05-rate-limit-idempotencia
# ou só o projeto de testes:
dotnet test labs/F5-M05-rate-limit-idempotencia/tests/F5M05.Api.Tests
dotnet run --project labs/F5-M05-rate-limit-idempotencia/src/F5M05.Api   # http://localhost:5505 (veja F5M05.Api.http)
```

No início, os **30 testes falham**. Ordem sugerida:

1. `RateLimiting/PoliticasDeLimite.cs` — opções de cada algoritmo (`AlgoritmosTests`).
2. Mesma classe: chaves de partição e `AddLimitesDeTaxa`; `.RequireRateLimiting(...)` em `Catalogo/`, `Pedidos/` e `Relatorios/` (`RateLimitTests`).
3. `EscreverRejeicaoAsync`: 429 + `Retry-After` + ProblemDetails (`RateLimitTests`).
4. `Idempotencia/ArmazemDeIdempotenciaEmMemoria.cs` (`ArmazemDeIdempotenciaTests`).
5. `Idempotencia/IdempotenciaMiddleware.cs` + `.ExigirIdempotencia()` no POST (`IdempotenciaApiTests`).
6. `.CacheOutput(...)` e `EvictByTagAsync` em `Catalogo/CatalogoEndpoints.cs` (`OutputCacheTests`).

**Por que os testes são determinísticos:** os limitadores de `System.Threading.RateLimiting` não aceitam `TimeProvider` (a reposição usa timer e relógio reais). Os testes de API usam janelas de 1 hora — nada é reposto durante o teste e o resultado depende só da contagem. A reposição é exercitada em `AlgoritmosTests` com `AutoReplenishment = false` + `TryReplenish()`. Concorrência é coordenada com `TaskCompletionSource` (nada de `Sleep`) e a expiração de chaves usa `FakeTimeProvider`.

Prontos (leia, não altere): `Program.cs` (ordem do pipeline), `Autenticacao/` (API key), `Catalogo/Catalogo.cs`, `Pedidos/Pedidos.cs`, `Relatorios/Relatorios.cs`, `RateLimiting/LimitesDeTaxaOptions.cs` e `Idempotencia/Contratos.cs`. **Não altere os testes.**
