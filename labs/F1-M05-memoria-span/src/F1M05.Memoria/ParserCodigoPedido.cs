using System.Globalization;

namespace F1M05.Memoria;

/// <summary>
/// Parser e formatador de <see cref="CodigoPedido"/> que trabalham sobre <see cref="ReadOnlySpan{T}"/>
/// e <see cref="Span{T}"/> sem alocar no heap (nada de Split, Substring, ToUpper ou string intermediária).
/// </summary>
public static class ParserCodigoPedido
{
    /// <summary>Menor ano aceito.</summary>
    public const int AnoMinimo = 2000;
    /// <summary>Maior ano aceito.</summary>
    public const int AnoMaximo = 2099;

    /// <summary>
    /// Tenta interpretar um código no formato <c>PPP-AAAA-NNNNNN</c>.
    /// Regras: ignora espaços nas pontas; prefixo "PED" ou "DEV" sem diferenciar maiúsculas/minúsculas;
    /// separadores '-' nas posições 3 e 8; ano com 4 dígitos entre <see cref="AnoMinimo"/> e
    /// <see cref="AnoMaximo"/>; sequencial com 6 dígitos maior que zero (sem sinal).
    /// NÃO pode alocar memória no heap, nem quando o texto é inválido.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> texto, out CodigoPedido codigo)
    {
        // Dicas: texto.Trim(), texto[..3].Equals("PED", StringComparison.OrdinalIgnoreCase),
        // texto.Slice(4, 4) e int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out var n).
        throw new NotImplementedException("TODO: valide tamanho, separadores, prefixo, ano e sequencial usando só fatias (slices) do span");
    }

    /// <summary>
    /// Como <see cref="TryParse"/>, mas lança <see cref="FormatException"/> quando o texto é inválido.
    /// </summary>
    public static CodigoPedido Parse(ReadOnlySpan<char> texto)
    {
        throw new NotImplementedException("TODO: reutilize TryParse e lance FormatException quando falhar");
    }

    /// <summary>
    /// Escreve o código formatado (ex.: <c>PED-2026-000123</c>) no <paramref name="destino"/>, sem alocar.
    /// Retorna <c>false</c> (e <paramref name="escritos"/> = 0) se o destino tiver menos de
    /// <see cref="CodigoPedido.Tamanho"/> caracteres.
    /// </summary>
    public static bool TryFormat(CodigoPedido codigo, Span<char> destino, out int escritos)
    {
        // Dica: int.TryFormat(destino.Slice(...), out _, "D6", CultureInfo.InvariantCulture) escreve com zeros à esquerda.
        throw new NotImplementedException("TODO: copie o prefixo, os '-' e formate ano (D4) e sequencial (D6) direto no destino");
    }

    /// <summary>
    /// Conta quantos códigos válidos existem em uma linha separada por <paramref name="separador"/>
    /// (ex.: <c>"PED-2026-000001;lixo;DEV-2026-000002"</c> → 2). Campos vazios são ignorados.
    /// NÃO pode alocar (nada de <c>string.Split</c>): percorra a linha com <c>IndexOf</c> e fatias (slices).
    /// </summary>
    public static int ContarValidos(ReadOnlySpan<char> linha, char separador = ';')
    {
        throw new NotImplementedException("TODO: laço com linha.IndexOf(separador), TryParse de cada campo e avance a fatia");
    }
}
