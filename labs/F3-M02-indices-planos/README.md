# F3-M02 — Índices e Planos de Execução

**Módulo:** Índices e Planos de Execução (Fase 3)
**Tempo:** 2 horas
**Requer Docker** (SQL Server 2022 em container via Testcontainers).

A fixture sobe um SQL Server, cria o banco `F3M02OrderFlow` e carrega uma massa determinística (20 mil clientes, 200 mil pedidos, 500 mil itens) em ~5 s com `INSERT ... SELECT` sobre `GENERATE_SERIES`. Depois aplica o **seu** `Indices.sql` e executa as consultas críticas com `SET STATISTICS XML ON` (plano real) e `SET STATISTICS IO ON` (leituras lógicas). Os testes inspecionam o XML do plano: `Index Seek` em vez de `Clustered Index Scan`, nenhum `Key Lookup`, nenhum `Sort`, índice filtrado em uso, nenhum aviso `CONVERT_IMPLICIT`, e leituras lógicas abaixo de um limite.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F3-M02-indices-planos
```

Com a imagem já baixada, a suíte roda em ~15–20 s.

O que você completa:

1. `src/F3M02.Indices/Sql/Indices.sql` — os índices (máximo 4 não clusterizados em `Pedidos`, nenhum redundante), cada um com o comentário de justificativa.
2. `src/F3M02.Indices/Sql/Consultas/04-FaturamentoDoMes.sql` — tornar a consulta SARGável (função na coluna).
3. `src/F3M02.Indices/Sql/Consultas/05-ClientePorDocumento.sql` — eliminar a conversão implícita `varchar` x `nvarchar`.

As consultas `01` a `03` **não** devem ser alteradas: o exercício é o índice. Os testes de "resultado continua o mesmo" das consultas 04 e 05 já passam no início (são a rede de proteção da reescrita) e precisam continuar verdes. **Não altere os testes.** No início 14 dos 17 testes falham.
