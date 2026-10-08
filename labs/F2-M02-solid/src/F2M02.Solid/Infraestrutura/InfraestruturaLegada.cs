using F2M02.Solid.Dominio;

namespace F2M02.Solid.Infraestrutura;

// Simulações em memória da infraestrutura "de verdade". No sistema real, cada uma abriria
// conexão com SQL Server, SMTP e Kafka. Aqui elas guardam o que receberam para os testes
// de caracterização conseguirem observar os efeitos colaterais. NÃO altere este arquivo.

public sealed class BancoDeDadosSql
{
    private readonly Dictionary<Guid, Produto> _produtos = [];
    private readonly List<Pedido> _pedidos = [];

    /// <summary>Quando true, <see cref="InserirPedido"/> falha (simula banco fora do ar).</summary>
    public bool SimularFalha { get; set; }

    public IReadOnlyList<Pedido> Pedidos => _pedidos;

    public void InserirProduto(Produto produto) => _produtos[produto.Id] = produto;

    public Produto? BuscarProduto(Guid id) => _produtos.GetValueOrDefault(id);

    public void InserirPedido(Pedido pedido)
    {
        if (SimularFalha)
        {
            throw new InvalidOperationException("Timeout ao conectar no SQL Server");
        }

        _pedidos.Add(pedido);
    }
}

public sealed record EmailEnviado(string Para, string Assunto, string Corpo);

public sealed class ServidorSmtp
{
    private readonly List<EmailEnviado> _enviados = [];

    public IReadOnlyList<EmailEnviado> Enviados => _enviados;

    public void Enviar(string para, string assunto, string corpo) => _enviados.Add(new EmailEnviado(para, assunto, corpo));
}

public sealed record MensagemKafka(string Topico, string Chave, string Valor);

public sealed class ProdutorKafka
{
    private readonly List<MensagemKafka> _mensagens = [];

    public IReadOnlyList<MensagemKafka> Mensagens => _mensagens;

    public void Produzir(string topico, string chave, string valor) => _mensagens.Add(new MensagemKafka(topico, chave, valor));
}
