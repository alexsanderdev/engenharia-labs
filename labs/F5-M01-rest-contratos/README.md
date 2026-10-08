# F5-M01 — HTTP REST e Contratos de API

**Módulo:** HTTP REST e Contratos de API (Fase 5)
**Tempo:** 2–3 horas

O OrderFlow tem uma API "RPC disfarçada de REST" (`Legado/RpcEndpoints.cs`): verbos na URL, tudo via POST, **200 para tudo** com `{ "sucesso": false }` no corpo, a entidade de domínio serializada direto (vazando `custoInterno` e `notaInternaAntifraude`) e erros em texto livre. Você vai substituí-la por uma API REST madura: recursos e sub-recursos (`/pedidos/{id}/itens`), métodos com a semântica certa (PUT/DELETE idempotentes), status codes certos (201 + Location, 204, 304, 400, 404, 409, 412, 415, 422), DTOs de contrato separados do domínio, `ETag` + `If-None-Match` (304) e `If-Match` (412, concorrência otimista), paginação com metadados e links, filtros e ordenação por query string e PATCH com **JSON Merge Patch** (RFC 7396).

```bash
dotnet test labs/F5-M01-rest-contratos
# ou só o projeto de testes:
dotnet test labs/F5-M01-rest-contratos/tests/F5M01.Api.Tests
dotnet run --project labs/F5-M01-rest-contratos/src/F5M01.Api   # http://localhost:5501 (veja F5M01.Api.http)
```

No início, os **65 casos de teste falham**. Ordem sugerida:

1. `Contratos/PedidoContratos.cs` — mapeamentos domínio → contrato (`De`). Depois `Http/PedidosEndpoints.cs`: `POST /pedidos`, `GET /pedidos/{id}`, `GET /pedidos/{id}/itens` (`RecursosEStatusTests`).
2. `Http/ETags.cs` — ETag forte, `If-None-Match` (comparação fraca) e `If-Match` (comparação forte) (`ETagsTests`); depois 304/412 nos endpoints (`CacheEConcorrenciaTests`).
3. Endpoints de escrita: `PUT`/`DELETE /pedidos/{id}/itens/{produtoId}`, `POST /pedidos/{id}/confirmacao` e `/cancelamento` (`RecursosEStatusTests`, `CacheEConcorrenciaTests`).
4. `Http/MergePatch.cs` — RFC 7396 (`MergePatchTests`); depois `PATCH /pedidos/{id}` (`PatchTests`).
5. `Http/ConsultaPedidos.cs` — validação, filtro, ordenação, paginação e links; depois `GET /pedidos` (`PaginacaoTests`).
6. `Program.cs` — pare de mapear o legado RPC.

Já vêm prontos: domínio (`Dominio/`), repositórios em memória, `Http/Problemas.cs` (toda resposta de erro em ProblemDetails com `code`), a validação dos dados de entrega (fim de `PedidosEndpoints.cs`) e o `Program.cs` com `AddProblemDetails` + `traceId`.

Os testes leem o JSON "cru" (`JsonElement`) de propósito: eles verificam o **contrato que o cliente vê** (status, headers e corpo), não os tipos C# do servidor. **Não altere os testes.**
