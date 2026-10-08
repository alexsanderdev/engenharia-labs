namespace F4M07.Patterns.Pagamentos.PagaFacil;

// ============================================================================================
// "SDK" DE TERCEIRO — PRONTO, NÃO ALTERE. Simula o pacote NuGet do gateway PagaFácil.
// Repare como a API é estranha para o nosso domínio: valor em CENTAVOS como string, moeda pelo
// código numérico ISO 4217 ("986" = BRL), status numérico, códigos de recusa de adquirente e
// exceção própria para timeout. É exatamente isso que o Adapter esconde.
// ============================================================================================

/// <summary>Requisição do PagaFácil.</summary>
public sealed class PagaFacilChargeRequest
{
    /// <summary>Valor em centavos, como texto. Ex.: R$ 19,99 → "1999".</summary>
    public required string AmountInCents { get; init; }

    /// <summary>Código numérico ISO 4217. BRL = "986".</summary>
    public required string CurrencyCode { get; init; }

    /// <summary>Referência do lojista (até 32 caracteres).</summary>
    public required string MerchantReference { get; init; }

    public required string CardHash { get; init; }
}

/// <summary>Resposta do PagaFácil.</summary>
public sealed class PagaFacilChargeResponse
{
    /// <summary>0 = aprovado, 1 = negado, 9 = erro interno do gateway.</summary>
    public int Status { get; init; }

    /// <summary>Preenchido quando aprovado.</summary>
    public string? AuthCode { get; init; }

    /// <summary>Código da adquirente quando negado: "51" saldo, "14" cartão inválido, "59" suspeita de fraude, outros.</summary>
    public string? DeclineCode { get; init; }
}

/// <summary>O PagaFácil não respondeu a tempo.</summary>
public sealed class PagaFacilTimeoutException(string message) : Exception(message);

/// <summary>
/// Cliente do PagaFácil. Na vida real faria HTTP; aqui recebe um "transporte" simulado no construtor.
/// Note: API SÍNCRONA (sim, existem SDKs assim) e sem <see cref="CancellationToken"/>.
/// </summary>
public sealed class PagaFacilClient(Func<PagaFacilChargeRequest, PagaFacilChargeResponse> transporte)
{
    public PagaFacilChargeResponse Charge(PagaFacilChargeRequest request) => transporte(request);
}
