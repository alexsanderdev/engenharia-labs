namespace F5M06.Pagamentos.Gateway;

/// <summary>Dados de uma cobrança, no vocabulário do OrderFlow.</summary>
public sealed record SolicitacaoCobranca(Guid PedidoId, decimal Valor, string Moeda, string TokenCartao);

/// <summary>Por que o gateway não pôde responder (falhas técnicas, possivelmente temporárias).</summary>
public enum MotivoIndisponibilidade
{
    /// <summary>Tentativa ou orçamento total estourou (<c>TimeoutRejectedException</c>).</summary>
    Timeout,
    /// <summary>Circuito aberto: a chamada nem saiu (<c>BrokenCircuitException</c>).</summary>
    CircuitoAberto,
    /// <summary>Conexão recusada/derrubada, resposta malformada (<c>HttpRequestException</c>).</summary>
    FalhaDeRede,
    /// <summary>5xx depois de esgotar as tentativas.</summary>
    ErroNoGateway,
    /// <summary>429 depois de esgotar as tentativas.</summary>
    LimiteDeRequisicoes,
    /// <summary>2xx com corpo fora do contrato (JSON inválido/incompleto): não sabemos o que aconteceu.</summary>
    RespostaInvalida,
}

/// <summary>Por que o gateway rejeitou a requisição (não adianta repetir igual).</summary>
public enum MotivoRejeicao
{
    DadosInvalidos,
    NaoAutorizado,
    ConflitoDeIdempotencia,
    Desconhecido,
}

/// <summary>
/// Resultado de uma cobrança. O caso de uso faz <c>switch</c> sobre ele; nenhuma exceção de HTTP
/// (<c>HttpRequestException</c>, <c>TimeoutRejectedException</c>, <c>BrokenCircuitException</c>) atravessa a fronteira.
/// </summary>
public abstract record ResultadoCobranca
{
    private ResultadoCobranca() { }

    /// <summary>Cobrança aprovada (201).</summary>
    public sealed record Aprovada(string TransacaoId) : ResultadoCobranca;

    /// <summary>O emissor recusou o cartão (402). Resultado de NEGÓCIO: não é falha técnica.</summary>
    public sealed record Recusada(string Motivo) : ResultadoCobranca;

    /// <summary>O gateway rejeitou a requisição (4xx). Bug ou configuração: repetir não resolve.</summary>
    public sealed record Rejeitada(MotivoRejeicao Motivo) : ResultadoCobranca;

    /// <summary>Não deu para saber/obter resposta. A cobrança PODE ou não ter acontecido: reconcilie depois.</summary>
    public sealed record Indisponivel(MotivoIndisponibilidade Motivo) : ResultadoCobranca;
}

/// <summary>Status de uma cobrança no gateway.</summary>
public enum StatusPagamento
{
    Desconhecido,
    Pendente,
    Aprovado,
    Recusado,
    Estornado,
}

/// <summary>De onde veio a resposta da consulta de status.</summary>
public enum OrigemStatus
{
    /// <summary>Resposta do gateway agora.</summary>
    Gateway,
    /// <summary>Fallback: último status conhecido (o gateway falhou).</summary>
    UltimoConhecido,
    /// <summary>Fallback: sem histórico, devolvemos <see cref="StatusPagamento.Desconhecido"/>.</summary>
    Padrao,
}

/// <summary>Resultado de uma consulta de status (nunca lança por falha do gateway).</summary>
public sealed record ConsultaStatus(string TransacaoId, StatusPagamento Status, OrigemStatus Origem)
{
    public bool Degradado => Origem != OrigemStatus.Gateway;
}

/// <summary>Resultado de um estorno.</summary>
public sealed record ResultadoEstorno(bool Sucesso, MotivoIndisponibilidade? Motivo = null);
