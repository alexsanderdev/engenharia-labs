# F5-M04 — Autenticação e Autorização

**Módulo:** Autenticação e Autorização (Fase 5)
**Tempo:** 2,5 horas

Minimal API do OrderFlow (catálogo e pedidos) protegida com **JWT Bearer** validado de verdade (emissor, audiência, assinatura RS256, `exp`/`nbf` com tolerância de 30 s e relógio injetável), **políticas** (papel `Admin`, papel `Cliente`, escopo `pedidos.write`, `FallbackPolicy` exigindo login) e **autorização baseada em recurso** (`AuthorizationHandler<OperationAuthorizationRequirement, Pedido>`) contra BOLA/IDOR (OWASP API1:2023). Os testes usam `WebApplicationFactory`, um emissor de tokens com par RSA gerado **em memória** e `FakeTimeProvider`: nada de Entra ID, rede ou segredo em arquivo.

```bash
dotnet test labs/F5-M04-authn-authz
# ou só o projeto de testes:
dotnet test labs/F5-M04-authn-authz/tests/F5M04.Api.Tests
dotnet run --project labs/F5-M04-authn-authz/src/F5M04.Api   # http://localhost:5504 (veja F5M04.Api.http)
```

No início, os 40 casos de teste falham (19 métodos). Ordem sugerida:

1. `Seguranca/ConfigurarJwtBearer.cs` — `MapInboundClaims = false`, `TokenValidationParameters` completo e `ValidarJanelaDeValidade` (`AutenticacaoTests`).
2. `Seguranca/AutorizacaoExtensions.cs`, `Seguranca/EscopoHandler.cs` e as políticas nos endpoints (`Catalogo/ProdutoEndpoints.cs`, mapeamento em `Pedidos/PedidoEndpoints.cs`) (`PoliticasTests`).
3. `Pedidos/PedidoAuthorizationHandler.cs` — dono lê e cancela, Admin só lê (`PedidoAuthorizationHandlerTests`).
4. `Pedidos/PedidoEndpoints.cs` — `Obter` e `Cancelar` com `IAuthorizationService`: 404 para quem não pode ler, 403 para quem lê mas não pode agir (`PedidoAutorizacaoTests`).

Já vêm prontos: domínio e repositórios em memória, `Program.cs` (ordem do pipeline, `AddProblemDetails` + `UseStatusCodePages` para 401/403/404 virarem ProblemDetails), `AutenticacaoOptions`, `GET /me` e, **só em Development**, `POST /dev/token` (chave efêmera para usar o `.http`). Nos testes, `Infra/EmissorDeTokensDeTeste.cs` e `Infra/ApiFactory.cs` trazem os helpers `ComoAdmin()`, `ComoCliente(id, escopos)`, `ComTokenExpirado()` e `Anonimo()`.

**Não altere os testes.** A configuração real com Microsoft Entra ID (`Microsoft.Identity.Web`) é opcional e está nas notas Aula (seção 6) e Lab (Passo 5) do módulo, sem nenhum segredo versionado.
