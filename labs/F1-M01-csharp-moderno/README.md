# F1-M01 — C# Moderno

**Módulo:** C-Sharp Moderno (Fase 1)
**Tempo:** 1h30–2h

Modela o núcleo do OrderFlow com recursos modernos do C#: `Dinheiro` como value object (record e igualdade por valor), `Produto` com `required`/`init`/`with` e a palavra-chave `field`, `StatusPedido` com switch expressions, `Pedido` com primary constructor e regras de desconto com property/relational/list patterns, e um `Catalogo` com nullable reference types e collection expressions.

```bash
dotnet test labs/F1-M01-csharp-moderno/tests/F1M01.CSharpModerno.Tests
```

1. `Dinheiro.cs` — construtor, operadores e `ToString` (testes `DinheiroTests`).
2. `Produto.cs` — validação no `init`, `ComPreco`, `Desativar`, `Resumo` (`ProdutoTests`).
3. `StatusPedido.cs` — `PodeTransicionar` e `Descrever` (`StatusPedidoTests`).
4. `Pedido.cs` — `Subtotal`, `AdicionarItem`, `AlterarStatus` (`PedidoTests`).
5. `RegrasDeDesconto.cs` — `PercentualPara` e `TotalComDesconto` (`RegrasDeDescontoTests`).
6. `Catalogo.cs` — `BuscarPorNome`, `NomeOuPadrao`, `Ativos`, `Mesclar` (`CatalogoTests`).

**Não altere os testes.** Roteiro completo na nota `C-Sharp Moderno - Lab` do vault. Gabarito na branch `solucoes`.
