namespace F3M03.Transacoes.Estoque;

/// <summary>Resultado de uma tentativa de reservar (dar baixa em) estoque de um produto.</summary>
public enum ResultadoReserva
{
    /// <summary>A baixa foi gravada.</summary>
    Reservado,

    /// <summary>Não havia estoque suficiente; nada foi alterado.</summary>
    EstoqueInsuficiente,

    /// <summary>
    /// Concorrência otimista: outra transação alterou a linha entre a leitura e a escrita
    /// em todas as tentativas permitidas; nada foi alterado.
    /// </summary>
    Conflito,
}
