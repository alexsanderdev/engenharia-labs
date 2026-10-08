# MP2 — Legado "OrderCalc"

**Mini-projeto:** MP2 — Refatoração de Legado (Fase 2) · **Tempo:** ~6–8 h (a parte OrderCalc do MP2)

Um serviço de cálculo de pedido de ~380 linhas, do jeito que ele aparece na vida real:

- **God class**: `OrderCalculator.Calcular` valida, calcula desconto, cupom, frete, imposto, prazo e ainda grava.
- **Primitive obsession**: itens chegam como `"SKU:QTD;SKU:QTD"`, tipos são strings mágicas (`"VIP"`, `"PERCENT"`), dinheiro é `decimal` solto.
- **Dependência estática de tempo**: `DateTime.Now` chamado 12 vezes (Black Friday, validade de cupom, prazo, número do pedido).
- **"Banco" estático**: `Db` com dicionários públicos e mutáveis (trate como infraestrutura que você **não pode mudar de formato**).
- **Duplicação com divergência**: `CalcularOrcamento` é uma cópia "ajustada" de `Calcular`. As diferenças são bugs ou regras? Ninguém sabe.

```bash
dotnet test projetos/MP2-ordercalc-legado
```

Este é um **projeto aberto**: os 2 testes de exemplo já passam. Não existe "fazer os testes ficarem verdes".
A sua régua é: **nenhum comportamento muda sem você decidir que ele deve mudar**.

## Missão (nesta ordem)

1. **Caracterize antes de tocar.** Escreva testes de caracterização (golden master) que fotografem o comportamento
   atual: totais, mensagens, erros e a **ordem** das validações, efeitos no `Db` (estoque, cupom, cliente `NOVO`, pedidos gravados).
   Para incluir `Numero`, `CriadoEm` e `PrazoEntrega`, você precisa do primeiro seam: controlar o tempo.
2. **Quebre dependências (seams).** A refatoração mínima e segura: um construtor que recebe `TimeProvider`
   (o construtor sem parâmetros continua usando `TimeProvider.System`). Teste com `FakeTimeProvider` (já referenciado).
   Depois, coloque o `Db` atrás de interfaces (catálogo, clientes, cupons, registro de pedidos).
3. **Extraia value objects**: `Money` (atenção ao arredondamento!), `Quantidade`, `Uf`. Enums no lugar de strings mágicas.
4. **Remova a duplicação** entre `Calcular` e `CalcularOrcamento` sem perder as divergências que você caracterizou
   (torne-as explícitas no código).
5. **Adicione uma regra nova com TDD**: entrega `"RETIRADA"` na loja, só para SP, frete zero, prazo de 1 dia útil.

Regras do jogo:

- Um smell por commit (`refactor: extrai Money`, `refactor: injeta TimeProvider`…). Rode os testes a cada passo.
- Os testes de caracterização **nunca** podem ficar vermelhos no meio do caminho. Se ficaram, você mudou comportamento: desfaça.
- `OrderCalculator`, `ResultadoPedido` e `Db` mantêm as assinaturas públicas (outros sistemas dependem deles). Por dentro, tudo pode mudar.
- O `[assembly: CollectionBehavior(DisableTestParallelization = true)]` existe por causa do estado estático. Ele é um sintoma.

## Approval tests: `Aprovacao` e Verify

`tests/MP2.OrderCalc.Tests/Aprovacao.cs` é um golden master de bolso: serializa o resultado em JSON e compara com
`<Classe>.<Metodo>.approved.txt`. Se diferente, grava `.received.txt` (ignorado pelo git) e falha. Para aprovar,
**revise** o `.received.txt` e renomeie para `.approved.txt`.

A ferramenta profissional para isso é o **Verify** (`Verify.XunitV3`, já no `Directory.Packages.props`).
A partir da versão 33, o Verify exige que o projeto declare a situação de patrocínio/licença
(erro `SC021` no build, com as opções listadas na mensagem). Essa declaração é uma decisão **sua**
(leia https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md). Depois de decidir, adicione no `.csproj` de testes:

```xml
<PackageReference Include="Verify.XunitV3" />
<!-- + a propriedade escolhida, ex.: <Verify_SponsorshipExemption>...</Verify_SponsorshipExemption> -->
```

e use `return Verify(resultado);` (com `using static VerifyXunit.Verifier;`) no lugar de `Aprovacao.Verificar`.

## Cobertura e mutação

```bash
dotnet test projetos/MP2-ordercalc-legado --collect:"XPlat Code Coverage;Format=cobertura" --results-directory ./TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:"Html;TextSummary"
```

Repare: só com os 2 exemplos, a cobertura de linhas já passa de 80%, mas a de **branches** fica abaixo de 50%.
Cobertura alta não prova caracterização. Use os branches como guia para os cenários que faltam.

## Gabarito

Na branch `solucoes`: versão refatorada (`Dominio/`, `Aplicacao/`, `Infra/`, fachada `OrderCalculator`),
suíte de caracterização completa (63 cenários de pedido + 21 de orçamento, gerada a partir do legado),
testes de unidade dos value objects e regras, e a regra `RETIRADA` feita com TDD. Compare **depois** de terminar.

## Mutation testing

```bash
cd projetos/MP2-ordercalc-legado
dotnet stryker   # usa stryker-config.json (runner "mtp", necessário para xUnit v3)
```

Com só os 2 testes de exemplo, o mutation score fica em ~29%. Meta do MP2: ≥ 70% depois da caracterização.
