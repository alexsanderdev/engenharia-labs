namespace F5M01.Api.Dominio;

public enum StatusPedido { Created, Confirmed, Completed, Cancelled }

/// <summary>Tipo de violação de regra. A borda HTTP decide o status (o domínio não conhece HTTP).</summary>
public enum TipoErro
{
    /// <summary>A operação conflita com o estado atual do recurso (ex.: confirmar pedido já confirmado) → 409.</summary>
    Conflito,

    /// <summary>A requisição é válida, mas o conteúdo viola uma regra de negócio (ex.: produto inativo) → 422.</summary>
    RegraDeNegocio,
}

/// <summary>Erro de domínio com código estável (o <c>code</c> do ProblemDetails).</summary>
public sealed record ErroDominio(string Code, string Mensagem, TipoErro Tipo);

public static class ErrosPedido
{
    public static ErroDominio TransicaoInvalida(StatusPedido de, StatusPedido para) =>
        new("pedido.transicao_invalida", $"Não é possível passar o pedido de {de} para {para}.", TipoErro.Conflito);

    public static ErroDominio NaoEditavel(StatusPedido status) =>
        new("pedido.nao_editavel", $"Pedido com status {status} não pode ser alterado.", TipoErro.Conflito);

    public static ErroDominio ProdutoInativo(Produto produto) =>
        new("produto.inativo", $"O produto '{produto.Nome}' está inativo e não pode entrar em pedidos.", TipoErro.RegraDeNegocio);

    public static readonly ErroDominio ProdutoInexistente =
        new("produto.inexistente", "Produto não encontrado no catálogo.", TipoErro.RegraDeNegocio);
}

/// <summary>Produto do catálogo. <see cref="Custo"/> é informação interna (margem) e NUNCA deve sair na API.</summary>
public sealed record Produto(Guid Id, string Nome, decimal Preco, bool Ativo, decimal Custo);

public sealed record Endereco(string Logradouro, string Cidade, string Cep);

public sealed class ItemPedido
{
    internal ItemPedido(Produto produto, int quantidade)
    {
        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        PrecoUnitario = produto.Preco;
        CustoUnitario = produto.Custo;
        Quantidade = quantidade;
    }

    public Guid ProdutoId { get; }
    public string NomeProduto { get; }
    public decimal PrecoUnitario { get; }
    public decimal CustoUnitario { get; }
    public int Quantidade { get; internal set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>
/// Agregado Pedido (PRONTO). Repare em <see cref="Versao"/>: cada mudança EFETIVA incrementa a versão.
/// É dela que sai o ETag — e é por isso que um PUT repetido (sem mudança) não muda o ETag.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public DateTimeOffset CriadoEm { get; }
    public StatusPedido Status { get; private set; } = StatusPedido.Created;
    public int Versao { get; private set; } = 1;
    public string? Observacao { get; private set; }
    public Endereco? EnderecoEntrega { get; private set; }

    /// <summary>Uso interno do antifraude. Detalhe de implementação: não pertence ao contrato público.</summary>
    public string? NotaInternaAntifraude { get; private set; } = "score=0.12;regra=R7";

    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Total => _itens.Sum(i => i.Subtotal);

    /// <summary>Soma dos custos (margem do negócio). Interno.</summary>
    public decimal CustoInterno => _itens.Sum(i => i.CustoUnitario * i.Quantidade);

    public static Pedido Criar(Guid clienteId, DateTimeOffset agora)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Cliente obrigatório.", nameof(clienteId));
        return new Pedido(Guid.NewGuid(), clienteId, agora);
    }

    /// <summary>
    /// Define a quantidade de um produto no pedido (cria o item se não existir). Idempotente:
    /// repetir com a mesma quantidade não muda nada (nem a versão).
    /// </summary>
    /// <param name="criado">true se o item não existia.</param>
    public ErroDominio? DefinirItem(Produto produto, int quantidade, out bool criado)
    {
        ArgumentNullException.ThrowIfNull(produto);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        criado = false;

        if (Status != StatusPedido.Created) return ErrosPedido.NaoEditavel(Status);

        var existente = _itens.Find(i => i.ProdutoId == produto.Id);
        if (existente is not null)
        {
            if (existente.Quantidade == quantidade) return null; // nada mudou: não incrementa versão
            existente.Quantidade = quantidade;
            Versao++;
            return null;
        }

        if (!produto.Ativo) return ErrosPedido.ProdutoInativo(produto);

        _itens.Add(new ItemPedido(produto, quantidade));
        criado = true;
        Versao++;
        return null;
    }

    /// <summary>Remove o item, se existir. Remover o que não existe não é erro (o estado final é o mesmo).</summary>
    public ErroDominio? RemoverItem(Guid produtoId)
    {
        if (Status != StatusPedido.Created) return ErrosPedido.NaoEditavel(Status);
        if (_itens.RemoveAll(i => i.ProdutoId == produtoId) > 0) Versao++;
        return null;
    }

    public ErroDominio? AtualizarDadosEntrega(string? observacao, Endereco? endereco)
    {
        if (Status is StatusPedido.Completed or StatusPedido.Cancelled) return ErrosPedido.NaoEditavel(Status);
        if (observacao == Observacao && endereco == EnderecoEntrega) return null;
        Observacao = observacao;
        EnderecoEntrega = endereco;
        Versao++;
        return null;
    }

    public ErroDominio? Confirmar() => Transicionar(StatusPedido.Created, StatusPedido.Confirmed);

    public ErroDominio? Concluir() => Transicionar(StatusPedido.Confirmed, StatusPedido.Completed);

    public ErroDominio? Cancelar()
    {
        if (Status is StatusPedido.Completed or StatusPedido.Cancelled)
            return ErrosPedido.TransicaoInvalida(Status, StatusPedido.Cancelled);
        Status = StatusPedido.Cancelled;
        Versao++;
        return null;
    }

    private ErroDominio? Transicionar(StatusPedido esperado, StatusPedido novo)
    {
        if (Status != esperado) return ErrosPedido.TransicaoInvalida(Status, novo);
        if (novo == StatusPedido.Confirmed && _itens.Count == 0)
            return new ErroDominio("pedido.sem_itens", "Pedido sem itens não pode ser confirmado.", TipoErro.RegraDeNegocio);
        Status = novo;
        Versao++;
        return null;
    }
}
