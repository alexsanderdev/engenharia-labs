# F1-M04 — LINQ e Performance Básica

**Módulo:** LINQ e Performance Básica (Fase 1)
**Tempo:** 1h30–2h

Consultas do catálogo e de pedidos do OrderFlow com LINQ: filtro, ordenação, paginação, `GroupBy`, `Join`,
`ToLookup`, `Chunk`, `CountBy`/`AggregateBy`, execução adiada e múltipla enumeração.

```bash
dotnet test labs/F1-M04-linq/tests/F1M04.Linq.Tests
```

1. Rode os testes e veja-os falhar (21 testes).
2. Implemente, nesta ordem: `ConsultasCatalogo` → `ConsultasPedidos` → `ExecucaoAdiada` (em `src/F1M04.Linq/`).
3. Rode até ficar tudo verde. **Não altere os testes.** `EnumeravelContador<T>` (nos testes) acusa múltipla enumeração.
4. Opcional: rode o benchmark LINQ x loop (não tem testes e não conta para o lab ficar verde):

```bash
dotnet run -c Release --project labs/F1-M04-linq/benchmarks/F1M04.Linq.Benchmarks -- --filter "*"
```
