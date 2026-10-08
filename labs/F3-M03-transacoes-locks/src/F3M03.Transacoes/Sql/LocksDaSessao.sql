-- Locks de UMA sessão (@sessao) no banco atual, concedidos ou em espera.
-- Colunas (o C# lê pelo nome): TipoDeRecurso, Modo, Status, Tabela
--
-- Para OBJECT, resource_associated_entity_id é o object_id da tabela.
-- Para KEY/PAGE/RID/HOBT, é o hobt_id: junte com sys.partitions para chegar ao object_id.
-- Para DATABASE, não há tabela (NULL).
SELECT
    l.resource_type  AS TipoDeRecurso,
    l.request_mode   AS Modo,
    l.request_status AS Status,
    OBJECT_NAME(
        CASE WHEN l.resource_type = 'OBJECT' THEN l.resource_associated_entity_id ELSE p.object_id END) AS Tabela
FROM sys.dm_tran_locks AS l
LEFT JOIN sys.partitions AS p
    ON  p.hobt_id = l.resource_associated_entity_id
    AND l.resource_type IN ('KEY', 'PAGE', 'RID', 'HOBT')
WHERE l.request_session_id   = @sessao
  AND l.resource_database_id = DB_ID()
ORDER BY l.resource_type, l.request_mode;
