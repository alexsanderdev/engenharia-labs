# F5-M02 — Versionamento, ProblemDetails e OpenAPI

**Módulo:** Versionamento, ProblemDetails e OpenAPI (Fase 5)
**Tempo:** 2 horas

O OrderFlow precisa mudar o contrato de `GET /pedidos/{id}` de forma **incompatível** (status numérico → texto; `total` → `valorTotal { valor, moeda }`) sem quebrar quem usa a versão atual. Você vai:

- versionar a API com **Asp.Versioning** por **segmento de URL** (`/v1/...` e `/v2/...` convivendo no mesmo *version set*);
- marcar a v1 como **depreciada**, com `api-supported-versions`/`api-deprecated-versions`, `Deprecation` (RFC 9745), `Sunset` (RFC 8594) e `Link` para a política;
- padronizar **todo** erro em **ProblemDetails (RFC 9457)** com `code`, `traceId`, `type` e `instance` — inclusive 404 de rota, versão inexistente e exceção (500 sem vazamento);
- gerar **um documento OpenAPI por versão** (`/openapi/v1.json`, `/openapi/v2.json`) com transformers (título, versão, descrição com depreciação, esquema Bearer, `deprecated: true`, exemplos) e a UI **Scalar** só em Development.

```bash
dotnet test labs/F5-M02-versionamento-openapi
# ou só o projeto de testes:
dotnet test labs/F5-M02-versionamento-openapi/tests/F5M02.Api.Tests
dotnet run --project labs/F5-M02-versionamento-openapi/src/F5M02.Api   # http://localhost:5502/scalar/v2 (veja F5M02.Api.http)
```

No início, os **29 casos de teste falham**. Ordem sugerida:

1. `Erros/ProblemDetailsPadrao.cs` — `CodigoPadrao` e `Customizar` (`ProblemDetailsTests`).
2. `Configuracao/Versionamento.cs` — `AddApiVersioning` + políticas de depreciação/sunset + `AddApiExplorer`.
3. `Http/PedidosEndpoints.cs` — version set com v1 (depreciada) e v2 + metadados de OpenAPI (`VersionamentoTests`).
4. `OpenApi/DocumentacaoOpenApi.cs` e `OpenApi/Transformers.cs` — `AddOpenApi("v1")`, `AddOpenApi("v2")`, transformers, `MapOpenApi` e Scalar em Development (`OpenApiTests`).

Já vêm prontos: domínio e repositório em memória (com um pedido semeado), contratos v1/v2 (`Contratos/`), erros de negócio (`Erros/Problemas.cs`), handlers dos endpoints e o `Program.cs`.

Observação: o `.csproj` suprime os avisos `AV0029`/`AV0030` do analisador do Asp.Versioning, que sugerem o pacote `Asp.Versioning.OpenApi` (não disponível no `Directory.Packages.props`). Aqui os documentos por versão são registrados à mão — veja a Aula. **Não altere os testes.**
