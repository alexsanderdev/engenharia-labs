# F4-M07 — Design Patterns com critério

**Módulo:** Design Patterns (Fase 4)
**Tempo:** 2h–2h30

Seis padrões, cada um resolvendo um problema REAL do OrderFlow e registrado pelo DI da Microsoft, mais um
exercício de anti-padrão:

| Passo | Padrão | Problema do OrderFlow | Arquivos com TODO |
|---|---|---|---|
| 1 | Strategy (keyed services) | frete por modalidade sem `switch` | `Frete/Calculadoras.cs`, `Frete/CotadorDeFrete.cs`, `AddFrete` |
| 2 | Decorator | cache + log em volta do serviço de preços | `Precos/ServicoDePrecosComCache.cs`, `ServicoDePrecosComLog.cs`, `DecoracaoDeServicos.cs`, `AddPrecos` |
| 3 | Adapter | SDK estranho do gateway PagaFácil atrás de uma porta do domínio | `Pagamentos/PagaFacilAdapter.cs`, `AddPagamentos` |
| 4 | Factory | notificação de pedido confirmado por canal (e-mail, SMS, push) | `Notificacoes/FabricaDeNotificacoes.cs`, `AddNotificacoes` |
| 5 | Specification | regras de cupom combináveis com E/OU/NÃO | `Cupons/Especificacao.cs`, `RegrasDeElegibilidade.cs`, `RegrasDeCupom.cs` |
| 6 | Chain of Responsibility | validações de checkout em corrente | `Checkout/Validacoes.cs`, `PipelineDeValidacaoDoCheckout.cs`, `AddCheckout` |
| 7 | Anti-padrão | Singleton estático + Service Locator → DI explícita | `Checkout/CalculadoraDeTotalDoPedido.cs` |

```bash
dotnet test labs/F4-M07-design-patterns
# ou só o projeto de testes:
dotnet test labs/F4-M07-design-patterns/tests/F4M07.Patterns.Tests
```

1. Rode os testes e veja-os falhar (54 testes, 80 casos com as `Theory`).
2. Siga os passos na ordem da tabela. Cada classe de teste (`StrategyFreteTests`, `DecoratorPrecosTests`, ...) é um passo.
3. Rode até ficar tudo verde. **Não altere os testes.**

Arquivos prontos (leia, não altere): `*/Contratos.cs`, `Pagamentos/Porta.cs`, `Pagamentos/PagaFacilSdk.cs` (o "SDK de terceiro"),
`Precos/ServicoDePrecosDoCatalogo.cs`, `Notificacoes/Modelos.cs` e a pasta `Legado/` (o anti-padrão — no fim você pode apagá-la).

Sem MediatR, sem Scrutor: o `Decorar<TServico, TDecorator>()` você escreve em ~20 linhas.
Gabarito na branch `solucoes`.
