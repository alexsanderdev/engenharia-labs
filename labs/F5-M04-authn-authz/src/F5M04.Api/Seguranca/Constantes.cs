namespace F5M04.Api.Seguranca;

/// <summary>Nomes dos claims como chegam no JWT (com <c>MapInboundClaims = false</c>, nada é renomeado).</summary>
public static class TiposDeClaim
{
    public const string Sub = "sub";
    public const string Papel = "role";
    /// <summary>Escopos OAuth 2.0: UMA string separada por espaços (RFC 8693 §4.2).</summary>
    public const string Escopo = "scope";
    /// <summary>Variante usada pelo Microsoft Entra ID para escopos delegados.</summary>
    public const string EscopoEntra = "scp";
}

/// <summary>Papéis (RBAC) emitidos pelo provedor de identidade.</summary>
public static class Papeis
{
    public const string Admin = "Admin";
    public const string Cliente = "Cliente";
}

/// <summary>Escopos (permissões delegadas ao app cliente) que a API entende.</summary>
public static class Escopos
{
    public const string PedidosLeitura = "pedidos.read";
    public const string PedidosEscrita = "pedidos.write";
    public const string ProdutosEscrita = "produtos.write";
}

/// <summary>Nomes das políticas de autorização registradas em <see cref="AutorizacaoExtensions"/>.</summary>
public static class Politicas
{
    public const string Admin = "Admin";
    public const string Cliente = "Cliente";
    public const string PedidosEscrita = "Escopo:pedidos.write";
}
