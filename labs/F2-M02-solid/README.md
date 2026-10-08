# F2-M02 — SOLID sem dogma

**Módulo:** SOLID (Fase 2)
**Tempo:** 2 horas

Lab de **refatoração**. `Legado/PedidoService.cs` valida, carrega produtos, calcula desconto com `switch` de cupom,
grava no banco, manda e-mail e publica no Kafka — tudo num método, dependendo de classes concretas.
Você vai separar responsabilidades (SRP), trocar o `switch` por políticas de desconto (OCP/Strategy) que
respeitam um contrato (LSP), depender de interfaces pequenas (ISP) e só criar abstração onde há variação
ou teste (DIP com `IOrderEventPublisher`).

```bash
dotnet test labs/F2-M02-solid
# durante o trabalho:
dotnet test labs/F2-M02-solid/tests/F2M02.Solid.Tests
```

Dois tipos de teste:

- `ComportamentoPedidoServiceTests` — **caracterização** do serviço legado com a infraestrutura simulada.
  Começam verdes e **nunca podem ficar vermelhos**.
- `Design*Tests` — exercitam a arquitetura nova, com NSubstitute e fakes, sem infraestrutura. Começam vermelhos (29 falhas).

Ordem sugerida:

1. `Descontos/PoliticasDeDesconto.cs` — políticas e catálogo → `DesignDescontosTests`.
2. `Dominio/Pedido.cs` — `Pedido.Criar` (regra pura) → `DesignPedidoTests`.
3. `Aplicacao/CriarPedidoHandler.cs` e `ResumoDeComprasDoCliente.cs` → `DesignCriarPedidoHandlerTests`, `DesignResumoDeComprasTests`.
4. `Infraestrutura/Adaptadores.cs` → `DesignAdaptadoresTests`.
5. Reescreva `Legado/PedidoService.cs` como fachada que compõe as peças novas — a caracterização continua verde.

Já vêm prontos e não devem mudar: `Aplicacao/Portas.cs` (as interfaces), `Infraestrutura/InfraestruturaLegada.cs`
(banco/SMTP/Kafka simulados) e `SemDesconto`. Repare no que **não** tem interface: `Pedido`,
`CatalogoDePoliticasDeDesconto` e o próprio handler.

O lab termina quando **tudo** está verde. **Não altere os testes.**
