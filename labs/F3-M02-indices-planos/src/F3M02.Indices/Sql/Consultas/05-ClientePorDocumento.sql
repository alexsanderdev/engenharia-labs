-- 05 · Busca de cliente por CPF no atendimento (VOCÊ CORRIGE ESTA CONSULTA; o índice único já existe)
-- Parâmetro: @documento — chega como NVARCHAR(11), igual ao AddWithValue("@documento", "123...") do ADO.NET.
-- Colunas: Id, Nome, Email
-- TODO (Passo 6): o resultado está CERTO, mas o plano tem CONVERT_IMPLICIT na coluna Documento (varchar)
--                 e varre o índice. Faça a conversão do lado do PARÂMETRO.
SELECT c.Id, c.Nome, c.Email
FROM dbo.Clientes AS c
WHERE c.Documento = @documento;
