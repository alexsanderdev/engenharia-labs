using System.Text.Json.Serialization;

namespace F6M04.Pedidos.Dominio;

/// <summary>
/// PRONTO. Fato que o contexto de Pedidos publica para fora (contrato público, versionável).
/// Só tipos primitivos: quem consome não conhece o modelo interno de <see cref="Pedido"/>.
/// </summary>
public interface IEventoDeIntegracao
{
    /// <summary>Nome estável do evento no broker (vira a routing key e o <c>type</c> da mensagem).</summary>
    [JsonIgnore]
    string Tipo { get; }

    /// <summary>
    /// Chave que define a ORDEM: eventos com a mesma chave (o mesmo pedido) precisam sair na ordem em
    /// que aconteceram. Eventos de chaves diferentes podem sair em qualquer ordem.
    /// </summary>
    [JsonIgnore]
    string ChaveDeOrdenacao { get; }

    /// <summary>Quando o fato aconteceu (relógio do servidor).</summary>
    DateTimeOffset OcorridoEm { get; }
}

/// <summary>PRONTO. Um pedido foi criado.</summary>
public sealed record PedidoCriado(Guid PedidoId, string Numero, string ClienteEmail, decimal Total, DateTimeOffset OcorridoEm)
    : IEventoDeIntegracao
{
    public const string NomeDoTipo = "pedido.criado";

    [JsonIgnore] public string Tipo => NomeDoTipo;
    [JsonIgnore] public string ChaveDeOrdenacao => PedidoId.ToString();
}

/// <summary>PRONTO. Um pedido foi confirmado.</summary>
public sealed record PedidoConfirmado(Guid PedidoId, string Numero, string ClienteEmail, DateTimeOffset OcorridoEm)
    : IEventoDeIntegracao
{
    public const string NomeDoTipo = "pedido.confirmado";

    [JsonIgnore] public string Tipo => NomeDoTipo;
    [JsonIgnore] public string ChaveDeOrdenacao => PedidoId.ToString();
}

/// <summary>
/// PRONTO. Agregado que acumula eventos até ser salvo. Quem salva (o interceptor da Outbox) recolhe
/// os eventos e os grava na MESMA transação do agregado.
/// </summary>
public interface ITemEventos
{
    /// <summary>
    /// Versão do agregado: começa em 0 e sobe 1 a cada evento registrado. O evento N do agregado é gravado na
    /// Outbox com <c>VersaoDoAgregado = N</c>. Também é token de concorrência: duas transações não geram a mesma versão.
    /// </summary>
    int Versao { get; }

    IReadOnlyList<IEventoDeIntegracao> Eventos { get; }

    void LimparEventos();
}
