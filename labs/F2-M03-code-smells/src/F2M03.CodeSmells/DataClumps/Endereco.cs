namespace F2M03.CodeSmells.DataClumps;

/// <summary>
/// O "grupo de dados" (data clump) logradouro + número + cidade + UF + CEP, que viaja junto em toda assinatura,
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
        Logradouro = logradouro;
        Numero = numero;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
        throw new NotImplementedException("TODO: mova para cá a validação/normalização de UF e CEP de AgendaDeEntregas.Agendar.");
    }

    /// <summary>CEP no formato 00000-000.</summary>
    public string CepFormatado => throw new NotImplementedException("TODO: 00000-000.");

    /// <summary>"Logradouro, Número — Cidade/UF — CEP 00000-000".</summary>
    public string Linha => throw new NotImplementedException("TODO: monte a linha do endereço usada na etiqueta.");
}
