using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Fonte;
using F6M08.Consistencia.Mensageria;
using F6M08.Consistencia.Projecao;
using F6M08.Consistencia.Reconciliacao;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace F6M08.Consistencia.Tests.Infra;

/// <summary>
/// Monta fonte da verdade → fila com atraso → projetor → projeção, tudo com relógio falso.
/// <see cref="Avancar"/> anda o relógio e entrega à projeção o que ficou disponível: é assim que
/// os testes "abrem" e "fecham" a janela de inconsistência sem nenhum sleep.
/// </summary>
public sealed class Cenario
{
    public static readonly DateTimeOffset Inicio = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public Cenario(TimeSpan? atrasoDaProjecao = null, Action<ConsistenciaOptions>? configurar = null)
    {
        Opcoes = new ConsistenciaOptions { AtrasoDaProjecao = atrasoDaProjecao ?? TimeSpan.FromSeconds(3) };
        configurar?.Invoke(Opcoes);
        Fila = new FilaComAtraso<EventoDePedido>(Relogio, Opcoes.AtrasoDaProjecao);
        Fonte = new FonteDePedidos(Fila, Relogio);
        Projetor = new Projetor(Fila, Projecao);
    }

    public FakeTimeProvider Relogio { get; } = new(Inicio);
    public ConsistenciaOptions Opcoes { get; }
    public FilaComAtraso<EventoDePedido> Fila { get; }
    public FonteDePedidos Fonte { get; }
    public ProjecaoResumoDoCliente Projecao { get; } = new();
    public Projetor Projetor { get; }

    public ServicoDeConsulta Consulta() => new(Projecao, Fonte, Options.Create(Opcoes), Relogio);

    public Reconciliador Reconciliador() => new(Fonte, Projecao, Options.Create(Opcoes), Relogio);

    /// <summary>Anda o relógio e projeta o que chegou. Devolve quantos eventos foram consumidos.</summary>
    public int Avancar(TimeSpan quanto)
    {
        Relogio.Advance(quanto);
        return Projetor.ProcessarDisponiveis();
    }
}

public static class Eventos
{
    public static readonly Guid Ana = Guid.Parse("a0000000-0000-0000-0000-00000000000a");
    public static readonly Guid Bruno = Guid.Parse("b0000000-0000-0000-0000-00000000000b");

    public static PedidoCriado Criado(Guid pedido, decimal total = 100m, Guid? cliente = null) =>
        new(pedido, cliente ?? Ana, 1, Cenario.Inicio, total);

    public static ItemAdicionado Item(Guid pedido, long versao, decimal valor, Guid? cliente = null) =>
        new(pedido, cliente ?? Ana, versao, Cenario.Inicio, valor);

    public static PedidoConfirmado Confirmado(Guid pedido, long versao, Guid? cliente = null) =>
        new(pedido, cliente ?? Ana, versao, Cenario.Inicio);

    public static PedidoCancelado Cancelado(Guid pedido, long versao, Guid? cliente = null) =>
        new(pedido, cliente ?? Ana, versao, Cenario.Inicio, "desistência");

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Limite de segurança para awaits que DEVEM completar (evita teste pendurado se a implementação estiver errada).</summary>
    public static readonly TimeSpan LimiteDeSeguranca = TimeSpan.FromSeconds(5);
}
