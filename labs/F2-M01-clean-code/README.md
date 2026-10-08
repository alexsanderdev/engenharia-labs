# F2-M01 — Clean Code

**Módulo:** Clean Code (Fase 2)
**Tempo:** 1h30–2h

Lab de **refatoração**. `Legado/PedidoUtil.cs` calcula e valida pedidos num método só: nomes de uma letra,
flags booleanas (`f1`, `f2`), números mágicos, aninhamento de 5 níveis, retorno `-1` como erro e comentários
que mentem ("15% para VIP", "frete grátis acima de 200"). Você vai extrair uma API limpa —
`ValidadorDePedido`, `RegrasDeFrete` e `CalculadoraDePedido` — e transformar o legado numa fachada fina.

```bash
dotnet test labs/F2-M01-clean-code
# durante o trabalho:
dotnet test labs/F2-M01-clean-code/tests/F2M01.CleanCode.Tests
```

Dois tipos de teste:

- `ComportamentoLegadoTests` — **caracterização**: registram o que o legado faz hoje. Começam verdes e
  **nunca podem ficar vermelhos**, em nenhum passo.
- `Design*Tests` — exercitam a API nova. Começam vermelhos (28 falhas no início).

Ordem sugerida:

1. Leia `PedidoUtil.Calc` e os testes de caracterização. Anote cada smell.
2. `ValidadorDePedido` (todos os erros, na ordem) → `DesignValidadorDePedidoTests`.
3. `RegrasDeFrete` (guard clause, constantes nomeadas) → `DesignRegrasDeFreteTests`.
4. `CalculadoraDePedido` (fluxo que se lê como frase) → `DesignCalculadoraDePedidoTests`.
5. Reescreva `PedidoUtil.Calc` como fachada que delega para a calculadora → `DesignEquivalenciaTests`
   e a caracterização continuam verdes.

O lab termina quando **tudo** está verde. **Não altere os testes.** `Pedido.cs` (records e enums) já vem pronto.
