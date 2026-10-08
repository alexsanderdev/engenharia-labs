# F4-M02 — DDD tático

**Módulo:** DDD (Fase 4)
**Tempo:** 2h30–3h

Modelar o agregado **Pedido** do OrderFlow com DDD tático, sem banco e sem framework:
value objects (`Dinheiro`, `Quantidade`, `Sku`, `EnderecoDeEntrega`), id fortemente tipado (`PedidoId`),
entidade interna (`ItemDoPedido`), raiz protegendo invariantes, factory method, domain events acumulados
na raiz (`PedidoCriado`, `PedidoConfirmado`, `PedidoCancelado`) e um domain service (`PoliticaDeDesconto`).

```bash
dotnet test labs/F4-M02-ddd-tatico
```

No início, os **20 testes (60 casos)** falham. O lab termina quando todos ficam verdes **sem alterar os testes**.

| Arquivo | Situação |
|---|---|
| `Comum/Entidade.cs`, `Comum/IEventoDeDominio.cs`, `Comum/RegraDeNegocioVioladaException.cs` | pronto (leia) |
| `Regras.cs`, `Identidades.cs` (`ClienteId`, `ProdutoId`), `Pedidos/StatusPedido.cs`, `Pedidos/ProdutoDoCatalogo.cs`, `Pedidos/EventosDoPedido.cs`, `Descontos/PerfilDoCliente.cs` | pronto (leia) |
| `ValueObjects/*.cs`, `PedidoId.cs` | **você completa** (Passo 1) |
| `Pedidos/ItemDoPedido.cs`, `Pedidos/Pedido.cs` | **você completa** (Passos 2 a 5) |
| `Comum/RaizDeAgregado.cs` | **você completa** (Passo 4) |
| `Descontos/PoliticaDeDesconto.cs` | **você completa** (Passo 6) |

Ordem sugerida (uma classe de teste por vez):

1. `ValueObjectsTests` — value objects e `PedidoId`.
2. `ItensDoPedidoTests` — factory method, itens, snapshot de preço, limites, encapsulamento.
3. `CicloDeVidaDoPedidoTests` — transições e domain events.
4. `PoliticaDeDescontoTests` — domain service e o desconto que nunca deixa o total inconsistente.

Regras do jogo:

- Mantenha as assinaturas públicas (os testes usam). Métodos privados e classes novas são bem-vindos.
- O projeto `F4M02.Pedidos.Domain` não pode referenciar nenhum pacote: é domínio puro.
- Toda violação de regra lança `RegraDeNegocioVioladaException` com o código de `Regras`.

A branch `solucoes` traz a implementação de referência.
