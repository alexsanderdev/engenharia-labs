using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MP3.Notificacoes.Worker.Inbox;
using MP3.Notificacoes.Worker.Mensageria;

namespace MP3.Notificacoes.Worker.Saude;

/// <summary>
/// Readiness do consumidor: está recebendo mensagens? Erro recente do processor (conexão caiu, lock perdido)
/// deixa "Degraded" — continua pronto, mas aparece no painel.
/// </summary>
public sealed class ConsumidorHealthCheck(ConsumidorDeNotificacoes consumidor, TimeProvider relogio) : IHealthCheck
{
    public static readonly TimeSpan JanelaDeErroRecente = TimeSpan.FromMinutes(1);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!consumidor.EstaProcessando)
            return Task.FromResult(HealthCheckResult.Unhealthy("O consumidor não está processando mensagens."));

        if (consumidor.UltimoErroEm is { } erro && relogio.GetUtcNow() - erro < JanelaDeErroRecente)
            return Task.FromResult(HealthCheckResult.Degraded($"Erro do processor em {erro:O}."));

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

/// <summary>Readiness da inbox: sem banco não dá para garantir "uma notificação só", então não estamos prontos.</summary>
public sealed class InboxHealthCheck(InboxSql inbox) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conexao = await inbox.AbrirAsync(cancellationToken);
            await using var comando = new SqlCommand("SELECT 1", conexao);
            await comando.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy("Inbox (SQL Server) indisponível.", ex);
        }
    }
}
