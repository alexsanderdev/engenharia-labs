-- Quem bloqueia quem: uma linha por sessão bloqueada no banco atual.
-- Colunas (o C# lê pelo nome): SessaoBloqueada, SessaoBloqueadora, TipoDeEspera, TempoDeEsperaMs,
--                             TipoDeRecurso, ModoSolicitado, Tabela, ComandoBloqueado
--
-- sys.dm_exec_requests  -> o que está executando agora; blocking_session_id <> 0 = está esperando alguém.
-- sys.dm_tran_locks     -> o lock que a sessão ESPERA (request_status = 'WAIT'): tipo de recurso e modo.
-- sys.partitions        -> para KEY/PAGE/RID/HOBT, resource_associated_entity_id é um hobt_id; daqui sai o object_id.
-- sys.dm_exec_sql_text  -> o texto do comando bloqueado, a partir do sql_handle.
SELECT
    r.session_id          AS SessaoBloqueada,
    r.blocking_session_id AS SessaoBloqueadora,
    r.wait_type           AS TipoDeEspera,
    r.wait_time           AS TempoDeEsperaMs,
    l.resource_type       AS TipoDeRecurso,
    l.request_mode        AS ModoSolicitado,
    OBJECT_NAME(
        CASE WHEN l.resource_type = 'OBJECT' THEN l.resource_associated_entity_id ELSE p.object_id END,
        l.resource_database_id) AS Tabela,
    t.text                AS ComandoBloqueado
FROM sys.dm_exec_requests AS r
JOIN sys.dm_tran_locks AS l
    ON  l.request_session_id = r.session_id
    AND l.request_status     = 'WAIT'
LEFT JOIN sys.partitions AS p
    ON  p.hobt_id = l.resource_associated_entity_id
    AND l.resource_type IN ('KEY', 'PAGE', 'RID', 'HOBT')
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.blocking_session_id <> 0
  AND l.resource_database_id = DB_ID()
ORDER BY r.session_id;
