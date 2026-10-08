using F6M04.Pedidos.Aplicacao;
using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using F6M04.Pedidos.Outbox;

namespace F6M04.Pedidos.Legado;

/// <summary>Em que momento a versão ingênua publica.</summary>
public enum MomentoDaPublicacao
{
    /// <summary>Salva, depois publica. Bug: se publicar falhar (ou o processo cair), o evento se perde.</summary>
    DepoisDoCommit,

    /// <summary>Publica, depois salva. Bug: se salvar falhar, o mundo recebe um evento de algo que não existe.</summary>
    AntesDoCommit,
}

/// <summary>
/// PRONTO — NÃO CORRIJA. A versão "dual write" ingênua: escreve no banco e no broker como duas operações
/// independentes. Não existe ordem certa entre elas; os testes em <c>Demonstracoes/DualWriteTests</c> mostram
/// os dois jeitos de dar errado. Fica no projeto como material de estudo.
/// </summary>
public sealed class ServicoDePedidosDualWrite(
    PedidosDbContext db,
    IPublicadorDeMensagens publicador,
    TimeProvider relogio,
    MomentoDaPublicacao momento)
{
    public async Task<Guid> CriarAsync(CriarPedido comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var pedido = Pedido.Criar(comando.Numero, comando.ClienteEmail, comando.Total, relogio.GetUtcNow());

        // A versão ingênua publica ela mesma: tira os eventos do agregado (assim nada vai para a Outbox).
        var mensagens = OutboxMessage.DoAgregado(pedido).Select(m => m.ParaMensagemDeSaida()).ToList();
        pedido.LimparEventos();
        db.Pedidos.Add(pedido);

        if (momento == MomentoDaPublicacao.AntesDoCommit)
        {
            foreach (var m in mensagens) await publicador.PublicarAsync(m, ct);
            await db.SaveChangesAsync(ct); // se falhar aqui, o evento JÁ saiu
        }
        else
        {
            await db.SaveChangesAsync(ct);
            foreach (var m in mensagens) await publicador.PublicarAsync(m, ct); // se falhar aqui, o pedido JÁ existe
        }

        return pedido.Id;
    }
}
