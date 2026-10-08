namespace F5M06.Pagamentos.Gateway;

// ---------------------------------------------------------------------------
// Contrato HTTP do gateway (DTOs de fio). Ficam internos à integração:
// o resto do OrderFlow só enxerga os resultados de domínio (Resultados.cs).
//
//   POST /v1/cobrancas                       Idempotency-Key obrigatório
//        201 { transacaoId, status: "aprovada" }
//        402 { codigo, mensagem }            cartão recusado
//        400/422 dados inválidos · 401/403 chave inválida · 409 chave reutilizada com outro corpo
//        429/503 (com Retry-After) · 500 erro interno
//   GET  /v1/cobrancas/{id}                  200 { transacaoId, status } · 404
//   POST /v1/cobrancas/{id}/estornos         202 · sem Idempotency-Key (operação NÃO idempotente)
// ---------------------------------------------------------------------------

/// <summary>Corpo do POST de cobrança.</summary>
public sealed record CobrancaRequestDto(Guid PedidoId, decimal Valor, string Moeda, string TokenCartao);

/// <summary>Resposta de cobrança aprovada e de consulta de status.</summary>
public sealed record CobrancaResponseDto(string TransacaoId, string Status);

/// <summary>Corpo de erro do gateway (402, 4xx).</summary>
public sealed record ErroGatewayDto(string? Codigo, string? Mensagem);
