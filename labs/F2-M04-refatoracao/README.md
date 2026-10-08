# F2-M04 — Refatoração

**Módulo:** Refatoração (Fase 2)
**Tempo:** 2h–2h30

Um legado real do OrderFlow: `CalculadoraDePontos` (pontos de fidelidade por compra), com condicionais aninhadas,
números mágicos, strings no lugar de tipos e o mesmo bloco copiado três vezes. Você vai refatorar em passos pequenos,
protegido por um **teste de aprovação**, e no fim implementar uma regra nova (categoria **Diamante**).

```bash
dotnet test labs/F2-M04-refatoracao
# durante o trabalho, só o projeto de testes:
dotnet test labs/F2-M04-refatoracao/tests/F2M04.Refatoracao.Tests
```

- `ComportamentoCalculadoraDePontosTests` (caracterização, 8 casos): **passam desde o início**. O principal gera 720 combinações
  de entrada e compara com `ComportamentoCalculadoraDePontosTests.Calcular_MatrizDeCombinacoes_IgualAoAprovado.verified.txt`
  (versionado, já aprovado). Se a saída mudar, o teste grava um `.received.txt` ao lado e falha mostrando a primeira linha diferente.
- `DesignProgramaDeFidelidadeTests` (12 casos): a API nova (`ProgramaDeFidelidade`, `RegrasDaCategoria`) e a regra Diamante. Começam vermelhos.

## O ciclo

**Um passo → rodar os testes → commit.** Se a caracterização ficar vermelha, `git restore` e tente um passo menor.
O lab termina quando **tudo** está verde. **Não altere os testes nem o `.verified.txt`.**

## Approval testing e Verify

`Aprovacao.cs` é um approval test "de bolso" com as mesmas convenções do [Verify](https://github.com/VerifyTests/Verify)
(`.verified.txt` versionado, `.received.txt` ignorado pelo `.gitignore` via `*.received.*`). O pacote `Verify.XunitV3` está
no `Directory.Packages.props`, mas a versão 33.x exige uma propriedade MSBuild de licença/patrocínio (SponsorCheck, erro SC021)
para compilar. Se você tiver direito a uma isenção ou patrocinar o projeto, troque assim:

1. No `.csproj` de testes: `<PackageReference Include="Verify.XunitV3" />` e a propriedade de licença indicada na mensagem SC021.
2. No teste: `using static VerifyXunit.Verifier;`, método `public Task ...()` e `return Verify(saida.ToString());`.
3. O nome do arquivo `.verified.txt` é o mesmo: nada muda no snapshot.

Prática extra: kata [Gilded Rose](https://github.com/emilybache/GildedRose-Refactoring-Kata) (versão C#).
Roteiro completo: nota *Refatoração — Lab* no vault. Gabarito: branch `solucoes`.
