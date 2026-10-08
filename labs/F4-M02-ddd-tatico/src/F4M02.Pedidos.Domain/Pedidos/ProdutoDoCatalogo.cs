using F4M02.Pedidos.Domain.ValueObjects;

namespace F4M02.Pedidos.Domain.Pedidos;

/// <summary>
/// O que o contexto de Pedidos precisa saber de um produto do Catálogo <b>no momento</b> de colocá-lo
/// no pedido. Quem chama (o caso de uso) consulta o Catálogo e entrega esta foto ao agregado.
/// </summary>
/// <remarks>
/// PRONTO. Repare: o agregado <c>Pedido</c> não carrega o agregado <c>Produto</c> do Catálogo
/// (outra fronteira de consistência) — recebe só os dados de que precisa para decidir.
/// "Produto" aqui não é o mesmo conceito que "Produto" no Catálogo (ver módulo DDD Estratégico).
/// </remarks>
/// <param name="Id">Referência ao produto por id (nunca por objeto).</param>
/// <param name="Sku">Código do produto.</param>
/// <param name="Nome">Nome para exibir no pedido (fica copiado no item).</param>
/// <param name="Preco">Preço atual no catálogo (vira o preço unitário do item: snapshot).</param>
/// <param name="Ativo">Se o produto pode ser vendido agora.</param>
public sealed record ProdutoDoCatalogo(ProdutoId Id, Sku Sku, string Nome, Dinheiro Preco, bool Ativo);
