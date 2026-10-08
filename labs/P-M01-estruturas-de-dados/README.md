# P-M01 — Estruturas de Dados do zero

**Módulo:** Estruturas de Dados (Trilha Paralela)
**Tempo:** 3–4 horas (pode ser dividido em sessões de 1 estrutura)

Implemente do zero as estruturas que estão por trás das coleções do .NET e entenda o Big O de cada operação.

```bash
dotnet test labs/P-M01-estruturas-de-dados/tests/PM01.Estruturas.Tests
```

| Ordem | Arquivo | Equivalente .NET | Testes |
|---|---|---|---|
| 1 | `ListaDinamica.cs` | `List<T>` | `ListaDinamicaTests` |
| 2 | `Pilha.cs` | `Stack<T>` | `PilhaTests` |
| 3 | `FilaCircular.cs` | `Queue<T>` | `FilaCircularTests` |
| 4 | `MapaHash.cs` | `Dictionary<TKey,TValue>` | `MapaHashTests` |
| 5 | `MinHeap.cs` | `PriorityQueue<TElement,TPriority>` | `MinHeapTests` |
| 6 | `ArvoreBinariaDeBusca.cs` | `SortedSet<T>` (que é balanceada) | `ArvoreBinariaDeBuscaTests` |

1. Rode os testes e veja-os falhar.
2. Implemente uma estrutura por vez, na ordem da tabela (filtre com `--filter "FullyQualifiedName~ListaDinamica"`).
3. **Não altere os testes.** Não use a coleção .NET equivalente por dentro (exceto `List<T>` no heap e `Stack<T>` no percurso da árvore).
4. Gabarito na branch `solucoes`.
