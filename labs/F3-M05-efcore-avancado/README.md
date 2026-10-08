# F3-M05 — EF Core Avançado

**Módulo:** EF Core Avançado (Fase 3)
**Tempo:** 2 horas
**Requer Docker** (SQL Server 2022 em container via Testcontainers).

Recursos avançados do EF Core **sem perder o controle do SQL gerado**. Vários exercícios partem de uma consulta RUIM que já devolve o resultado certo: o teste falha porque mede os comandos enviados ao banco (um `DbCommandInterceptor` de teste, `CapturaDeSql`, guarda cada round-trip).

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F3-M05-efcore-avancado
```

Com a imagem já baixada, a suíte roda em ~15 s (um container; cada teste roda numa transação desfeita no fim).

O que você completa (nesta ordem — o Passo 1 é base para os testes que usam datas):

1. `Persistencia/Interceptadores/AuditoriaInterceptor.cs` — `SaveChangesInterceptor` com `TimeProvider` (`CriadoEm`/`AtualizadoEm`).
2. `Persistencia/Configuracoes/ProdutoConfiguration.cs` — value conversion (`Sku`) e owned type (`Dinheiro`).
3. `ExclusaoLogicaInterceptor.cs` + `PedidoConfiguration.cs` — soft delete com global query filter.
4. `Consultas/RelatorioPedidos.ListarResumosAsync` — eliminar **N+1** (projeção, 1 comando).
5. `Consultas/PedidoConsultas.ObterDoClienteComDetalhesAsync` — **explosão cartesiana** → `AsSplitQuery` (3 comandos).
6. `PedidoConsultas.ListarItensDoClienteAsync` (identity resolution) e `Consultas/ConsultasCompiladas.cs` (`EF.CompileAsyncQuery`).
7. `RelatorioPedidos.FaturamentoPorDiaAsync` — descer para SQL com `Database.SqlQuery<T>`.
8. `Manutencao/ManutencaoService.cs` — `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (1 comando, nada carregado).

Já vem pronto: entidades e value objects (`Dominio/`), `LojaDbContext`, demais configurações e a infraestrutura de teste (`tests/.../Infra/`: container, transação por teste, `CapturaDeSql`, helpers de seed).

**Não altere os testes.** No início os 14 falham; o lab termina quando todos ficam verdes. Gabarito na branch `solucoes`.
