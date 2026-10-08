namespace F4M06.Catalogo.Contracts;

/// <summary>
/// Contrato público SÍNCRONO do Catálogo. Outros módulos (ex.: Pedidos) consultam produtos
/// por aqui, nunca pelo DbContext ou pelas entidades do Catálogo.
/// A implementação é <c>internal</c> e mora no projeto <c>F4M06.Catalogo</c>.
/// </summary>
public interface ICatalogoApi
{
    /// <summary>
    /// Devolve o resumo dos produtos pedidos, EM LOTE (uma ida ao banco, não uma por produto).
    /// Ids que não existem no catálogo simplesmente não aparecem no resultado.
    /// </summary>
    Task<IReadOnlyList<ProdutoResumo>> ObterProdutosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}

/// <summary>
/// DTO do contrato: só o que os outros módulos precisam saber de um produto, no momento da consulta.
/// Não é a entidade <c>Produto</c> do Catálogo (essa é privada do módulo).
/// </summary>
public sealed record ProdutoResumo(Guid Id, string Nome, decimal Preco, bool Ativo);
