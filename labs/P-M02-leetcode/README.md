# P-M02 — LeetCode com foco profissional (12 semanas)

**Módulo:** LeetCode (Trilha Paralela)
**Tempo:** 1 problema por semana, 45–90 minutos cada (Fases 0 e 1)

Doze problemas clássicos, um arquivo por problema, agrupados por padrão. O enunciado (com nossas palavras), a semana, o padrão e a complexidade esperada estão no comentário XML de cada classe.

```bash
dotnet test labs/P-M02-leetcode/tests/PM02.LeetCode.Tests
# só o problema da semana:
dotnet test labs/P-M02-leetcode/tests/PM02.LeetCode.Tests --filter "FullyQualifiedName~TwoSumTests"
```

| Semana | Problema | Pasta / padrão |
|---:|---|---|
| 1 | Two Sum | `HashMap` |
| 2 | Valid Anagram | `HashMap` |
| 3 | Group Anagrams | `HashMap` |
| 4 | Valid Palindrome | `TwoPointers` |
| 5 | Container With Most Water | `TwoPointers` |
| 6 | Longest Substring Without Repeating Characters | `SlidingWindow` |
| 7 | Best Time to Buy and Sell Stock | `SlidingWindow` |
| 8 | Valid Parentheses | `Stack` |
| 9 | Daily Temperatures | `Stack` |
| 10 | Binary Search | `BinarySearch` |
| 11 | Search in Rotated Sorted Array | `BinarySearch` |
| 12 | Top K Frequent Elements | `Heap` |

Regras: **não altere os testes**; antes de codar, escreva no vault a complexidade-alvo; depois de passar, registre padrão, complexidade e uma alternativa (ver nota "LeetCode - Lab"). Gabarito na branch `solucoes`.
