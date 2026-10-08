# F2-M03 — Code Smells

**Módulo:** Code Smells (Fase 2)
**Tempo:** 1h30–2h

Cinco trechos legados do OrderFlow, cada um com um smell clássico, e a refatoração correspondente:

| Pasta | Smell | Refatoração | Testes de design |
|---|---|---|---|
| `PrimitiveObsession/` | CPF, e-mail e dinheiro como `string`/`decimal` | Value objects `Cpf`, `Email`, `Dinheiro` | `DesignPrimitiveObsessionTests` |
| `FeatureEnvy/` | `CalculadoraDeFrete` vive dos dados do `Pedido` | Move Method para `Pedido`, `ItemDoPedido`, `EnderecoDeEntrega` | `DesignFeatureEnvyTests` |
| `DataClumps/` | 9 parâmetros; endereço e janela andando juntos | `Endereco`, `JanelaDeEntrega`, parameter object `SolicitacaoDeEntrega` + `[Obsolete]` | `DesignDataClumpsTests` |
| `SwitchStatements/` | 3 `switch` sobre o mesmo enum | Polimorfismo (`MeioDePagamento`) + fábrica | `DesignSwitchStatementsTests` |
| `ShotgunSurgery/` | regra "1 a 10" copiada em 3 classes | `PoliticaDeQuantidade` injetada | `DesignShotgunSurgeryTests` |

```bash
dotnet test labs/F2-M03-code-smells
# durante o trabalho, só o projeto de testes:
dotnet test labs/F2-M03-code-smells/tests/F2M03.CodeSmells.Tests
```

1. Rode os testes: os `Comportamento*Tests` (caracterização, 26 casos) **passam**; os `Design*Tests` (30 casos) **falham**.
2. Para cada pasta: implemente o tipo novo, faça o código legado **delegar** para ele e rode os testes a cada passo.
3. Os testes de caracterização **nunca** podem ficar vermelhos no meio do caminho. Se ficarem, desfaça o último passo.
4. O lab termina quando **tudo** está verde. **Não altere os testes.**

Roteiro completo e dicas: nota *Code Smells — Lab* no vault. Gabarito: branch `solucoes`.
