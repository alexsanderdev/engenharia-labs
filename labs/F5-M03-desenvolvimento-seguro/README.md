# F5-M03 — Desenvolvimento Seguro

**Módulo:** Desenvolvimento Seguro (Fase 5)
**Tempo:** 2–3 horas

A API de Catálogo/Clientes/Pedidos do OrderFlow vem com **vulnerabilidades deliberadas**. Cada uma tem testes que *atacam* a API via `WebApplicationFactory` (ambiente **Production**) e falham no starter. Você corrige o código — **nunca os testes**.

```bash
dotnet test labs/F5-M03-desenvolvimento-seguro
# ou só o projeto de testes:
dotnet test labs/F5-M03-desenvolvimento-seguro/tests/F5M03.Api.Tests
dotnet run --project labs/F5-M03-desenvolvimento-seguro/src/F5M03.Api   # http://localhost:5503 (veja F5M03.Api.http)
```

Estado inicial: **50 casos vermelhos, 8 verdes**. Os verdes (`ComportamentoTests`, `Upload_ExatamenteNoLimite_Aceita`, `Desenvolvimento_NaoEnviaHsts`) protegem o caminho feliz e **não podem ficar vermelhos** em nenhum passo.

| # | Vulnerabilidade | Onde corrigir | Testes |
|---|---|---|---|
| 1 | Mass assignment (`IsAdmin`, `Preco`, `Total`, `Status` no corpo) | `Clientes/*`, `Pedidos/*` | `MassAssignmentTests` |
| 2 | Exposição excessiva (entidade na resposta) | `Clientes/*`, `Pedidos/*`, `Produtos/*` | `ExposicaoDeDadosTests` |
| 3 | Validação ausente (tamanho, faixa, formato, allowlist) | validadores + `ProdutoEndpoints` | `ValidacaoTests` |
| 4 | Erro vazando stack trace/SQL | `Infra/TratamentoDeErrosMiddleware.cs` | `ErrosTests` |
| 5 | Senha/token/CPF em log | `Seguranca/Mascaramento.cs`, `Infra/LogDeRequisicaoMiddleware.cs`, cadastro | `LogsTests`, `MascaramentoTests` |
| 6 | Headers de segurança ausentes (nosniff, CSP, HSTS) | `Seguranca/CabecalhosDeSegurancaMiddleware.cs`, `SegurancaExtensions.cs` | `CabecalhosTests` |
| 7 | CORS refletindo qualquer origem com credenciais | `Seguranca/SegurancaExtensions.cs` | `CorsTests` |
| 8 | Upload sem limite de tamanho/tipo | `Produtos/ProdutoEndpoints.cs` | `UploadTests` |

Prontos (leia, não altere): `Dominio/*` (entidades, repositórios em memória — o de produtos simula o erro do SQL Server —, `HashDeSenha`), `Validacao/ErrosDeValidacao.cs` e `Program.cs`.
