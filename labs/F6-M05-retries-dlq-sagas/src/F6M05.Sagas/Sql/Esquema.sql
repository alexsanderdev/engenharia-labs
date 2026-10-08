-- PRONTO. Estado da saga de pedido (um registro por pedido) e as mensagens que já mudaram esse estado.
-- A infraestrutura de teste executa este script uma vez no container.

CREATE TABLE dbo.SagaPedido
(
    PedidoId            uniqueidentifier  NOT NULL CONSTRAINT PK_SagaPedido PRIMARY KEY,
    ClienteId           uniqueidentifier  NOT NULL,
    Valor               decimal(18, 2)    NOT NULL,
    ItensJson           nvarchar(max)     NOT NULL,
    Status              tinyint           NOT NULL,   -- StatusSaga
    Passos              int               NOT NULL,   -- PassosDaSaga (flags)
    AutorizacaoId       nvarchar(100)     NULL,
    MotivoCancelamento  nvarchar(400)     NULL,
    PrazoPagamentoEm    datetimeoffset(3) NULL,       -- só preenchido em AguardandoPagamento
    CriadaEm            datetimeoffset(3) NOT NULL,
    AtualizadaEm        datetimeoffset(3) NOT NULL,
    Versao              int               NOT NULL    -- concorrência otimista (1 = primeira gravação)
);

-- O verificador de prazos procura "AguardandoPagamento com prazo vencido": índice filtrado pequeno.
CREATE INDEX IX_SagaPedido_PrazoPagamento
    ON dbo.SagaPedido (PrazoPagamentoEm)
    WHERE PrazoPagamentoEm IS NOT NULL;

-- Idempotência por MessageId: a PK impede registrar a mesma mensagem duas vezes para a mesma saga.
CREATE TABLE dbo.SagaPedidoMensagem
(
    PedidoId      uniqueidentifier  NOT NULL,
    MessageId     nvarchar(200)     NOT NULL,
    ProcessadaEm  datetimeoffset(3) NOT NULL,
    CONSTRAINT PK_SagaPedidoMensagem PRIMARY KEY (PedidoId, MessageId),
    CONSTRAINT FK_SagaPedidoMensagem_SagaPedido FOREIGN KEY (PedidoId) REFERENCES dbo.SagaPedido (PedidoId)
);
