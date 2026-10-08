# engenharia-labs

Labs práticos da trilha **Engenharia de Software — Pleno → Sênior** (.NET, SQL Server, Azure).
O roteiro de cada lab está no vault do Obsidian (nota `<Módulo> - Lab`).

## Como funciona

- Cada lab fica em `labs/<Fase>-<Módulo>-<nome>/`, com `src/` (código inicial com `TODO`) e `tests/` (testes que **começam falhando**).
- O lab termina quando `dotnet test labs/<lab>` fica verde **sem alterar os testes**.
- As soluções de referência ficam na branch `solucoes`. Consulte só **depois** de terminar.

```bash
# um lab (cada pasta de lab tem seu próprio .slnx)
dotnet test labs/F0-M01-hello

# todos
dotnet test EngenhariaLabs.slnx
```

## Requisitos

- .NET SDK 10 (ver `global.json`)
- Docker Desktop (a partir da Fase 2, para Testcontainers)
- Ferramentas locais: `dotnet tool restore` (Stryker, SonarScanner, ReportGenerator)

## Labs

| Lab | Módulo | Testes |
|---|---|---:|
| `F0-M01-hello` | Setup Profissional | 5 |
| `F1-M01-csharp-moderno` | C-Sharp Moderno | 29 |
| `F1-M02-generics-colecoes` | Generics e Coleções | 19 |
| `F1-M03-async-cancelamento` | Async Await e Cancelamento | 17 |
| `F1-M04-linq` | LINQ e Performance Básica | 21 |
| `F1-M05-memoria-span` | Memória, GC e Span | 26 |
| `F1-M06-di-generic-host` | Injeção de Dependência e Generic Host | 19 |
| `F1-M07-aspnetcore-api` | ASP.NET Core e APIs | 25 |
| `F1-M08-config-options-logging` | Configuração, Options e Logging | 18 |
| `P-M01-estruturas-de-dados` | Estruturas de Dados (paralelo) | 32 |
| `P-M02-leetcode` | LeetCode — 12 semanas (paralelo) | 72 |
| `F2-M01-clean-code` | Clean Code | 45 |
| `F2-M02-solid` | SOLID | 45 |
| `F2-M03-code-smells` | Code Smells | 56 |
| `F2-M04-refatoracao` | Refatoração (approval tests) | 20 |
| `F2-M05-tdd` | TDD (aceitação + seus testes) | 17 |
| `F2-M06-testes-unitarios` | Testes Unitários | 33 |
| `F2-M07-testes-integracao` | Testes de Integração (Docker/SQL Server) | 19 |
| `F2-M08-arquitetura-mutacao` | Arquitetura (NetArchTest) e Mutation (Stryker) | 18 |

## Projetos abertos

| Projeto | Mini-projeto |
|---|---|
| `projetos/MP2-ordercalc-legado` | MP2 — Refatoração de Legado (testes de exemplo já passam; meta é caracterizar e refatorar) |

A tabela cresce a cada onda de produção. Veja `CONVENCOES.md`.
