# F2-M06 — Testes Unitários

**Módulo:** Testes Unitários (Fase 2)
**Tempo:** 1h30–2h

O código de produção (`src/`) **já está pronto**: o caso de uso `CriarPedido` com repositório, catálogo,
publicador de eventos e `TimeProvider`, mais as regras de domínio de `Pedido` e `Produto`. **Não altere `src/`.**

O seu trabalho é construir a **infraestrutura de teste** que os 27 testes (33 casos) já escritos usam:

| Arquivo (em `tests/F2M06.TestesUnitarios.Tests/`) | O que é |
|---|---|
| `Builders/ProdutoBuilder.cs` | Test Data Builder de `Produto` |
| `Builders/PedidoBuilder.cs` | Test Data Builder de `Pedido` (passa pela fábrica do domínio) |
| `Dubles/RepositorioDePedidosEmMemoria.cs` | Fake em memória do repositório (verificação de estado) |
| `Dubles/CatalogoStub.cs` | Stub do catálogo com NSubstitute |
| `Dubles/VerificacoesDoPublicador.cs` | Verificações do mock do publicador com NSubstitute |

```bash
dotnet test labs/F2-M06-testes-unitarios
```

1. Rode os testes e veja-os falhar (33 casos).
2. Implemente, nesta ordem: `ProdutoBuilder` → `PedidoBuilder` → `RepositorioDePedidosEmMemoria` →
   (testes de domínio ficam verdes) → `CatalogoStub` → `VerificacoesDoPublicador`.
3. Rode até ficar tudo verde. **Não altere os testes nem o `src/`.**

A branch `solucoes` traz as peças de infraestrutura completas.
