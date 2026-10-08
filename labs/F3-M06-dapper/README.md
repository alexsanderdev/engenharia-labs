# F3-M06 — Dapper e Consultas de Leitura

**Módulo:** Dapper e Consultas de Leitura (Fase 3)
**Tempo:** 2 horas
**Requer Docker** (imagem `mcr.microsoft.com/mssql/server:2022-latest`).

"CQRS leve" no OrderFlow: o EF Core (`Escrita/LojaDbContext`) cria o schema e grava os dados; você escreve o lado de **leitura** com Dapper sobre as mesmas tabelas — read models achatados, SQL parametrizado, multi-mapping, `QueryMultiple`, um relatório agregado em arquivo `.sql` e paginação.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez
dotnet test labs/F3-M06-dapper
```

A fixture (pronta) sobe **um** container por execução, cria o schema e grava uma massa de dados fixa (`tests/.../Infra/DadosDeTeste.cs`). Os testes só leem, então rodam em paralelo; a suíte leva ~15 s.

O que você completa (nesta ordem):

1. `src/.../Leitura/ProdutoQueries.cs` — `ObterPorSkuAsync` (parâmetro), `BuscarPorNomeAsync` (parâmetro + escape do LIKE; os testes provam que a versão concatenada de `ConsultasInseguras` é vulnerável), `ObterPorIdsAsync` (listas no `IN` acima de 2.100 itens).
2. `src/.../Leitura/PedidoQueries.cs` — `ObterComItensAsync` e `ListarDoClienteComItensAsync` (multi-mapping com `splitOn`), `ObterPainelDoClienteAsync` (`QueryMultiple`), `ListarPaginadoAsync` (`OFFSET/FETCH` + total).
3. `src/.../Sql/RelatorioDeVendas.sql` — relatório de vendas por dia (o C# que lê o arquivo já está pronto).

Já vem pronto: entidades e `LojaDbContext` (escrita), read models (`Leitura/Modelos.cs`), `SqlArquivos`, o contraexemplo `ConsultasInseguras` e toda a infraestrutura de teste.

**Não altere os testes.** No início os 25 casos falham; o lab termina quando todos ficam verdes.

## Benchmark (opcional)

`benchmarks/F3M06.Dapper.Benchmarks` compara o mesmo relatório em EF Core (ingênuo e com `GroupBy` traduzido) e Dapper, com ~20 mil pedidos. Não tem testes; precisa de Docker:

```bash
dotnet run -c Release --project labs/F3-M06-dapper/benchmarks/F3M06.Dapper.Benchmarks -- --filter *
```
