# F1-M05 — Memória, GC e Span

**Módulo:** Memória, GC e Span (Fase 1)
**Tempo:** 1h30–2h

Parser de código de pedido (`PED-2026-000123`) com `ReadOnlySpan<char>` sem alocar, formatação com
`Span<char>`/`stackalloc`, buffer temporário com `ArrayPool<T>` e um tipo `IDisposable` que devolve o que alugou.

```bash
dotnet test labs/F1-M05-memoria-span/tests/F1M05.Memoria.Tests
```

1. Rode os testes e veja-os falhar (26 testes).
2. Implemente, nesta ordem: `ParserCodigoPedido` (TryParse → Parse → TryFormat → ContarValidos) → `Etiquetas` → `BufferDeEtiquetas`.
3. Rode até ficar tudo verde. **Não altere os testes.**

Alguns testes medem alocação com `GC.GetAllocatedBytesForCurrentThread()` (depois de um aquecimento).
Se um deles falhar com "esperado 0, obtido N", procure a string escondida: `Substring`, `Split`, `ToUpper`,
interpolação, `ToString()`, boxing.
