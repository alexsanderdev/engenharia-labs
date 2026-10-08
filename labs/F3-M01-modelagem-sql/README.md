# F3-M01 — Modelagem Relacional e SQL

**Módulo:** Modelagem Relacional e SQL (Fase 3)
**Tempo:** 2 horas
**Requer Docker** (os testes sobem um SQL Server 2022 de verdade com Testcontainers).

O "código" deste lab é SQL. Na **Parte A** você escreve o DDL do OrderFlow (Clientes, Produtos, Pedidos, ItensPedido) com tipos corretos, PK, FK, `UNIQUE`, `CHECK` e `DEFAULT`; os testes leem o schema pelo catálogo (`INFORMATION_SCHEMA`, `sys.*`) e tentam gravar dados inválidos para provar que o banco os recusa. Na **Parte B** você escreve 10 consultas (JOIN, `GROUP BY/HAVING`, `EXISTS`, `LEFT JOIN`, CTE, `ROW_NUMBER`, `SUM() OVER`, `DENSE_RANK`) sobre uma massa pequena e conhecida; os testes comparam o resultado linha a linha.

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest   # uma vez (~600 MB)
dotnet test labs/F3-M01-modelagem-sql
```

Com a imagem já baixada, a suíte roda em ~15 s (um container para tudo; a Parte A usa transações revertidas, a Parte B só lê).

O que você completa:

1. `src/F3M01.Sql/Sql/ParteA/Schema.sql` — os 4 `CREATE TABLE` (o contrato de colunas está no cabeçalho do arquivo).
2. `src/F3M01.Sql/Sql/ParteB/01..10-*.sql` — uma consulta por arquivo; cada cabeçalho diz colunas (aliases) e ordem.

Já vem pronto: a infra de testes (`tests/.../Infra`: container, bancos `F3M01Modelagem` e `F3M01Consultas`, massa da Parte B em `BaseConsultas.sql`) e o leitor de scripts `ScriptsSql.cs` (entende `GO`).

As Partes A e B são independentes (bancos separados): dá para começar pela B. **Não altere os testes.** No início os 62 casos falham; o lab termina quando todos ficam verdes.
