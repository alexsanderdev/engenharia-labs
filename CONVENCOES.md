# Convenções dos labs

## Estrutura

```
labs/F1-M03-async-cancelamento/
  F1-M03-async-cancelamento.slnx    solução do lab (permite: dotnet test labs/F1-M03-async-cancelamento)
  README.md                         objetivo, comando, passos curtos
  src/F1M03.Async/F1M03.Async.csproj
  src/F1M03.Async/*.cs              código inicial: compila, TODOs lançam NotImplementedException("TODO: ...")
  tests/F1M03.Async.Tests/F1M03.Async.Tests.csproj
  tests/F1M03.Async.Tests/*.cs      testes xUnit v3 + Shouldly (+ NSubstitute se necessário)
```

- Nome da pasta: `F<fase>-M<módulo 2 dígitos>-<slug>`; paralelo: `P-M<nn>-<slug>`.
- Nome dos projetos: `F<fase>M<nn>.<Nome>` e `F<fase>M<nn>.<Nome>.Tests`. Nunca repita nomes entre labs.
- Os `.csproj` ficam mínimos: `Directory.Build.props` já define TargetFramework, Nullable, xUnit, Shouldly e `using Xunit; using Shouldly;` para projetos `*.Tests`.
- Versões de pacotes só em `Directory.Packages.props` (Central Package Management). Para usar um pacote, adicione `<PackageReference Include="X" />` sem versão.
- Projetos web: `<Project Sdk="Microsoft.NET.Sdk.Web">`; testes com `Microsoft.AspNetCore.Mvc.Testing`.

## Regras de design de um lab

1. Cabe em 1–2 horas.
2. Os testes contam uma história: do conceito mais simples ao mais difícil. O nome de cada teste explica a regra (`Metodo_Cenario_Resultado`).
3. As mensagens de `NotImplementedException` dizem o que fazer.
4. Na `main`, o código **compila** e os testes **falham**. Na `solucoes`, todos passam.
5. Sem dependências externas na Fase 0–1 (nada de Docker/banco). Testes determinísticos: sem `Thread.Sleep` longo, use `TimeProvider`/`FakeTimeProvider` para tempo.
6. Comentários e mensagens em português, identificadores do domínio em português quando natural (Pedido, Produto), termos técnicos em inglês.
