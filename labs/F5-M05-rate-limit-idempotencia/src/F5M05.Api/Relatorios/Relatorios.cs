using F5M05.Api.Pedidos;

namespace F5M05.Api.Relatorios;

// ARQUIVO PRONTO — não precisa alterar.
// Um relatório "caro" (no mundo real: varre o banco por segundos). É o caso clássico
// do concurrency limiter: não importa quantos por minuto, importa quantos AO MESMO TEMPO.

public sealed record RelatorioDeVendas(int TotalDePedidos, DateTimeOffset GeradoEm);

public interface IGeradorDeRelatorio
{
    Task<RelatorioDeVendas> GerarAsync(CancellationToken ct);
}

public sealed class GeradorDeRelatorio(IRepositorioDePedidos pedidos, TimeProvider relogio) : IGeradorDeRelatorio
{
    public Task<RelatorioDeVendas> GerarAsync(CancellationToken ct) =>
        Task.FromResult(new RelatorioDeVendas(pedidos.Quantidade, relogio.GetUtcNow()));
}
