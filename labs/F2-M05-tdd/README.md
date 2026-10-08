# F2-M05 — TDD

**Módulo:** TDD (Fase 2)
**Tempo:** 2h–2h30

Construir com TDD a máquina de estados do pedido do OrderFlow:
Created → Confirmed → Completed; Created → Cancelled; Completed não cancela;
histórico de transições com data via `TimeProvider`.

```bash
dotnet test labs/F2-M05-tdd
```

O lab tem duas partes:

1. **Testes de aceitação** (`tests/F2M05.Tdd.Tests/Aceitacao/`) — já escritos, começam vermelhos e definem o "pronto". **Não altere.**
2. **Seus testes unitários** (`tests/F2M05.Tdd.Tests/MeusTestes/`) — você escreve, **um por vez**, em ciclos
   red → green → refactor, seguindo o "Roteiro de ciclos" da nota Lab. Um commit por ciclo
   (`test: ...` no red, `feat: ...` no green, `refactor: ...` no refactor).

Regras:

- Não escreva código de produção sem um teste vermelho pedindo por ele.
- Mantenha as assinaturas públicas de `Pedido` (os testes de aceitação usam). Classes novas são bem-vindas no refactor.
- O lab termina quando os testes de aceitação e os seus estão verdes — e o `git log` conta a história dos ciclos.

A branch `solucoes` traz uma implementação de referência **e** os testes unitários que o aluno escreveria (`MeusTestes/`).
