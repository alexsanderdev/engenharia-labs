namespace F4M01.Domain.Comum;

/// <summary>
/// Violação de uma regra de negócio (invariante do domínio).
/// A Api traduz esta exceção para 422 Unprocessable Entity; o domínio não sabe o que é HTTP.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
