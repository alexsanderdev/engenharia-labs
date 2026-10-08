# F2-M08 — Testes de Arquitetura e Mutation Testing

**Módulo:** Testes de Arquitetura e Mutation Testing (Fase 2)
**Tempo:** 2 horas

Mini solução em camadas do OrderFlow (`Domain`, `Application`, `Infrastructure`) com **violações de arquitetura de propósito** e uma **suíte de testes de domínio fraca** que passa, mas deixa mutantes vivos.

```bash
dotnet test labs/F2-M08-arquitetura-mutacao
```

## Parte 1 — Regras de arquitetura (NetArchTest)

`tests/F2M08.Arquitetura.Tests` tem 9 regras; no início **5 falham**, e a mensagem lista os tipos culpados. Você corrige o **código**, nunca as regras:

1. Domain depende de EF Core (`Pedido.ConfigurarMapeamento`) → mova para `Infrastructure/Persistencia/PedidoConfiguration.cs`.
2. Application depende de classes concretas da Infrastructure → use `IPedidoRepository`/`IProdutoRepository`.
3. Entidades com setter público → `private set` + métodos com intenção.
4. `CancelarPedidoProcessor` → `CancelarPedidoHandler`, `sealed`.

Depois apague as referências que sobraram nos `.csproj` (EF Core no Domain, Infrastructure na Application).

## Parte 2 — Mutation testing (Stryker.NET)

`tests/F2M08.Domain.Tests` **passa desde o início** (mutation score não vira teste xUnit). O objetivo é **acrescentar** testes até o Stryker chegar a **≥ 80%**:

```bash
cd labs/F2-M08-arquitetura-mutacao
dotnet tool restore        # uma vez, na raiz do repo
dotnet stryker             # usa stryker-config.json (runner MTP, break em 80%)
```

O relatório HTML fica em `StrykerOutput/<data>/reports/mutation-report.html` (ignorado pelo git). O comando termina com erro enquanto o score estiver abaixo de 80%.

Scores de referência: código inicial 25,7%; depois da Parte 1, só com a suíte fraca, 36%; gabarito 98%.

**Não altere nem apague os testes existentes.** O lab termina com os 9 testes de arquitetura verdes e `dotnet stryker` saindo sem erro.
