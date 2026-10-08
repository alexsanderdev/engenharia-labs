# F4-M08 — Result Pattern e Tratamento de Erros

**Módulo:** Result Pattern e Tratamento de Erros (Fase 4)
**Tempo:** 2 horas

API de Pedidos do OrderFlow em que **erros de negócio não são exceções**: os casos de uso devolvem `Result`/`Result<T>` com um `Error` tipado (Validation, NotFound, Conflict, Forbidden, Failure) e código estável (`pedido.transicao_invalida`), a borda HTTP traduz para **ProblemDetails (RFC 9457)** com o status certo (400/404/409/403/422) e um `IExceptionHandler` global transforma exceções inesperadas em 500 genérico, sem vazar mensagem nem stack trace, com `traceId`.

```bash
dotnet test labs/F4-M08-result-pattern
# ou só o projeto de testes:
dotnet test labs/F4-M08-result-pattern/tests/F4M08.Api.Tests
dotnet run --project labs/F4-M08-result-pattern/src/F4M08.Api   # http://localhost:5408 (veja F4M08.Api.http)
```

No início, os 29 casos de teste falham. Ordem sugerida:

1. `Resultados/Result.cs` e `Resultados/Error.cs` — invariantes, conversões implícitas, `Map`/`Bind`/`Match`, fábricas (`ResultTests`, `ErrorTests`).
2. `Pedidos/PedidosCasosDeUso.cs` — casos de uso devolvendo Result, sem `throw` de negócio (`CasosDeUsoTests`).
3. `Http/ErrorHttpMapping.cs` — `ErrorType` → status/título e `Error` → `ProblemDetails` com `code` e `errors` (`ErrorTests`, `ApiTests`).
4. `Infra/GlobalExceptionHandler.cs` — 500 genérico + log completo; `BadHttpRequestException` → 400 (`ApiTests`).

Já vêm prontos: domínio (`Pedido` devolvendo `Result`, catálogo de erros `PedidoErrors`), repositórios em memória, endpoints (`Http/PedidoEndpoints.cs`) e `Program.cs` (AddProblemDetails com `traceId`, AddExceptionHandler, UseExceptionHandler, UseStatusCodePages).

**Fez o MP1 (Biblioteca NuGet)?** Os testes usam os tipos de `F4M08.Api.Resultados`; você pode implementá-los delegando ao seu pacote ou, depois do lab verde, trocar o namespace pelo do seu `ResultKit` e ajustar os testes numa branch própria. **Não altere os testes na branch do lab.**
