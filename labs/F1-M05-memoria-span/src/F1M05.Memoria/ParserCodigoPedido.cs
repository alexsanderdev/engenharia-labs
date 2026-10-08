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
        codigo = default;
        texto = texto.Trim();

        if (texto.Length != CodigoPedido.Tamanho || texto[3] != '-' || texto[8] != '-')
            return false;

        TipoCodigo tipo;
        var prefixo = texto[..3];
        if (prefixo.Equals("PED", StringComparison.OrdinalIgnoreCase))
            tipo = TipoCodigo.Pedido;
        else if (prefixo.Equals("DEV", StringComparison.OrdinalIgnoreCase))
            tipo = TipoCodigo.Devolucao;
        else
            return false;

        // NumberStyles.None: só dígitos (sem sinal, sem espaços, sem separador de milhar).
        if (!int.TryParse(texto.Slice(4, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var ano)
            || ano is < AnoMinimo or > AnoMaximo)
            return false;

        if (!int.TryParse(texto.Slice(9, 6), NumberStyles.None, CultureInfo.InvariantCulture, out var sequencial)
            || sequencial <= 0)
            return false;

        codigo = new CodigoPedido(tipo, ano, sequencial);
        return true;
    }

    /// <summary>
    /// Como <see cref="TryParse"/>, mas lança <see cref="FormatException"/> quando o texto é inválido.
    /// </summary>
    public static CodigoPedido Parse(ReadOnlySpan<char> texto)
    {
        return TryParse(texto, out var codigo)
            ? codigo
            : throw new FormatException($"Código de pedido inválido: '{texto}'.");
    }

    /// <summary>
    /// Escreve o código formatado (ex.: <c>PED-2026-000123</c>) no <paramref name="destino"/>, sem alocar.
    /// Retorna <c>false</c> (e <paramref name="escritos"/> = 0) se o destino tiver menos de
    /// <see cref="CodigoPedido.Tamanho"/> caracteres.
    /// </summary>
    public static bool TryFormat(CodigoPedido codigo, Span<char> destino, out int escritos)
    {
        escritos = 0;
        if (destino.Length < CodigoPedido.Tamanho)
            return false;

        codigo.Prefixo.AsSpan().CopyTo(destino);
        destino[3] = '-';
        codigo.Ano.TryFormat(destino.Slice(4, 4), out _, "D4", CultureInfo.InvariantCulture);
        destino[8] = '-';
        codigo.Sequencial.TryFormat(destino.Slice(9, 6), out _, "D6", CultureInfo.InvariantCulture);

        escritos = CodigoPedido.Tamanho;
        return true;
    }

    /// <summary>
    /// Conta quantos códigos válidos existem em uma linha separada por <paramref name="separador"/>
    /// (ex.: <c>"PED-2026-000001;lixo;DEV-2026-000002"</c> → 2). Campos vazios são ignorados.
    /// NÃO pode alocar (nada de <c>string.Split</c>): percorra a linha com <c>IndexOf</c> e fatias (slices).
    /// </summary>
    public static int ContarValidos(ReadOnlySpan<char> linha, char separador = ';')
    {
        var validos = 0;

        while (!linha.IsEmpty)
        {
            var posicao = linha.IndexOf(separador);
            var campo = posicao < 0 ? linha : linha[..posicao];

            if (TryParse(campo, out _))
                validos++;

            linha = posicao < 0 ? ReadOnlySpan<char>.Empty :linha[(posicao + 1)..];
        }

        return validos;
    }
}
