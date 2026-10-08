namespace F2M03.CodeSmells.DataClumps;

/// <summary>
/// O "grupo de dados" (data clump) logradouro + número + cidade + UF + CEP, que viajava junto em toda assinatura,
/// promovido a um tipo com validação e normalização num lugar só.
/// </summary>
public sealed record Endereco
{
    public string Logradouro { get; }
    public string Numero { get; }
    public string Cidade { get; }

    /// <summary>UF em maiúsculas (2 letras).</summary>
    public string Uf { get; }

    /// <summary>CEP só com os 8 dígitos.</summary>
    public string Cep { get; }

    /// <summary>Cria um endereço validado.</summary>
    /// <exception cref="ArgumentException">UF sem 2 letras ou CEP sem 8 dígitos.</exception>
    public Endereco(string logradouro, string numero, string cidade, string uf, string cep)
    {
        var ufNormalizada = (uf ?? string.Empty).Trim().ToUpperInvariant();
        if (ufNormalizada.Length != 2 || !ufNormalizada.All(char.IsAsciiLetter))
            throw new ArgumentException("UF deve ter 2 letras.", nameof(uf));

        var digitosCep = new string((cep ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digitosCep.Length != 8)
            throw new ArgumentException("CEP deve ter 8 dígitos.", nameof(cep));

        Logradouro = logradouro;
        Numero = numero;
        Cidade = cidade;
        Uf = ufNormalizada;
        Cep = digitosCep;
    }

    /// <summary>CEP no formato 00000-000.</summary>
    public string CepFormatado => $"{Cep[..5]}-{Cep[5..]}";

    /// <summary>"Logradouro, Número — Cidade/UF — CEP 00000-000".</summary>
    public string Linha => $"{Logradouro}, {Numero} — {Cidade}/{Uf} — CEP {CepFormatado}";
}
