# F6-M07 — Redis

**Módulo:** Redis (Fase 6)
**Tempo:** 2–3 horas · **Requer Docker** (imagem `redis:7.4-alpine`)

Cache do catálogo do OrderFlow num Redis real (Testcontainers): chaves versionadas (`catalogo:v1:produto:{id}`), TTL com jitter, **cache-aside** com `StackExchange.Redis` (JSON, cache negativo, fallback quando o Redis cai), **invalidação** ao atualizar, proteção contra **cache stampede** com lock distribuído (`SET NX PX` + token + script Lua), **HybridCache** (L1 memória + L2 Redis, stampede embutido, invalidação por tag), um **rate limiter distribuído** (`INCR` + `PEXPIRE` atômicos) e uma **medição** de leituras na fonte com e sem cache.

```bash
docker pull redis:7.4-alpine   # uma vez
dotnet test labs/F6-M07-redis
# ou só o projeto de testes:
dotnet test labs/F6-M07-redis/tests/F6M07.Cache.Tests
```

No início, os **19 testes falham**. Ordem sugerida:

1. `Cache/ChavesDeCache.cs` — formato da chave (`FundamentosTests`).
2. `Cache/PoliticaDeExpiracao.cs` — TTL com jitter (`FundamentosTests`).
3. `Cache/CatalogoComCacheAside.cs` — `ObterAsync` + `CarregarEGravarAsync` (`CacheAsideTests`, fallback em `FundamentosTests`).
4. Mesma classe — `AtualizarAsync` (invalidação).
5. `Cache/BloqueioDistribuido.cs` + `CarregarComBloqueioAsync` (stampede) (`BloqueioELimiteTests`, `CacheAsideTests`).
6. `Cache/CatalogoHibrido.cs` — `AddCatalogoHibrido` e os três métodos (`HybridCacheTests`).
7. `Limites/LimitadorDistribuido.cs` — janela fixa no Redis (`BloqueioELimiteTests`).

**Infra pronta (leia, não altere):** `tests/.../Infra/RedisFixture.cs` sobe UM Redis por execução (porta aleatória) e abre UM `ConnectionMultiplexer`; os testes usam ids, clientes e prefixos únicos, então não há limpeza entre eles. `FonteContadora` é a "fonte da verdade" fake que conta as leituras (é assim que se prova o ganho do cache) e simula latência. O rate limiter usa `FakeTimeProvider` para trocar de janela sem esperar. A suíte roda em ~5 s com a imagem baixada.

Prontos em `src`: `Catalogo/Produto.cs` (record + `IFonteDeProdutos`), scripts Lua e o esqueleto das classes. **Não altere os testes.**
