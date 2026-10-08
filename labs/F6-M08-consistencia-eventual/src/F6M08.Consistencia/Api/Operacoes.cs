using System.Collections.Concurrent;
using F6M08.Consistencia.Fonte;
using F6M08.Consistencia.Mensageria;

namespace F6M08.Consistencia.Api;

public enum StatusDaOperacao
{
    Processando,
    Concluida,
    Falhou,
}

/// <summary>Estado de uma operação assíncrona (o recurso apontado pelo <c>Location</c> do 202).</summary>
public sealed record Operacao(
    Guid OperacaoId, Guid ClienteId, StatusDaOperacao Status, Guid? PedidoId = null, long? Versao = null, string? Erro = null);

/// <summary>Comando enfileirado pelo <c>POST /pedidos</c> e processado em segundo plano.</summary>
public sealed record ComandoCriarPedido(Guid OperacaoId, Guid ClienteId, decimal Total);

/// <summary>Registro em memória das operações (em produção: uma tabela, ou o próprio pedido com status). Pronto.</summary>
public sealed class RegistroDeOperacoes
{
    private readonly ConcurrentDictionary<Guid, Operacao> operacoes = new();

    public Operacao Iniciar(Guid operacaoId, Guid clienteId)
    {
        var operacao = new Operacao(operacaoId, clienteId, StatusDaOperacao.Processando);
        if (!operacoes.TryAdd(operacaoId, operacao)) throw new InvalidOperationException($"Operação {operacaoId} já existe.");
        return operacao;
    }

    public void Concluir(Guid operacaoId, Gravacao gravacao) =>
        operacoes.AddOrUpdate(operacaoId,
            _ => throw new KeyNotFoundException(),
            (_, o) => o with { Status = StatusDaOperacao.Concluida, PedidoId = gravacao.PedidoId, Versao = gravacao.Versao });

    public void Falhar(Guid operacaoId, string erro) =>
        operacoes.AddOrUpdate(operacaoId,
            _ => throw new KeyNotFoundException(),
            (_, o) => o with { Status = StatusDaOperacao.Falhou, Erro = erro });

    public Operacao? Obter(Guid operacaoId) => operacoes.GetValueOrDefault(operacaoId);
}

/// <summary>
/// Consome os comandos de criação (depois de <c>AtrasoDoProcessamento</c>) e grava na fonte da verdade.
/// Pronto: leia, não altere.
/// </summary>
public sealed class ProcessadorDeComandos(
    FilaComAtraso<ComandoCriarPedido> fila,
    FonteDePedidos fonte,
    RegistroDeOperacoes registro,
    ILogger<ProcessadorDeComandos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await fila.Leitor.WaitToReadAsync(stoppingToken))
            {
                while (fila.Leitor.TryRead(out var comando))
                {
                    try
                    {
                        registro.Concluir(comando.OperacaoId, fonte.Criar(comando.ClienteId, comando.Total));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Falha ao processar a operação {OperacaoId}.", comando.OperacaoId);
                        registro.Falhar(comando.OperacaoId, ex.Message);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // desligamento normal
        }
    }
}
