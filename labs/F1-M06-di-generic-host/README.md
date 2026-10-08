# F1-M06 — Injeção de Dependência e Generic Host

**Módulo:** Injeção de Dependência e Generic Host (Fase 1)
**Tempo:** 1h30–2h

Composição do OrderFlow em extension methods com lifetimes corretos, decorator manual, factory,
keyed services, `IOptions` básico, validação do container (`ValidateOnBuild`/`ValidateScopes`) e um
`BackgroundService` testável com `FakeTimeProvider`.

```bash
dotnet test labs/F1-M06-di-generic-host/tests/F1M06.Hosting.Tests
```

1. Rode os testes e veja-os falhar (19 testes).
2. Implemente, nesta ordem: `ServicoDePrecosComCache` e `GeradorDeCodigoPedido` → `ComposicaoOrderFlow`
   (`AddCatalogo`, `AddFrete`, `AddPedidos`) → `ValidacaoDoContainer` → `OrderFlowHost` → `LimpezaDePedidosExpirados`.
3. Rode até ficar tudo verde. **Não altere os testes.** `ComposicaoErrada.cs` é o exemplo do que NÃO fazer — não mexa nele.
