using System.Net;
using System.Net.Sockets;

namespace F5M06.Pagamentos.Tests.Infra;

/// <summary>
/// Simula falha de REDE (não de HTTP): aceita a conexão TCP e a derruba na hora com RST.
/// O HttpClient recebe <c>HttpRequestException</c>, como num load balancer reiniciando ou num pod morrendo.
/// O WireMock simula respostas HTTP; para "o cabo caiu" um socket cru é mais fiel e é instantâneo.
/// (Infra pronta: você não precisa alterar este arquivo.)
/// </summary>
public sealed class ServidorQueDerrubaConexoes : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private int _conexoes;

    public ServidorQueDerrubaConexoes()
    {
        _listener.Start();
        _ = AceitarEDerrubarAsync();
    }

    public Uri Url => new($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

    /// <summary>Quantas conexões (tentativas) chegaram.</summary>
    public int Conexoes => Volatile.Read(ref _conexoes);

    private async Task AceitarEDerrubarAsync()
    {
        try
        {
            while (true)
            {
                using var socket = await _listener.AcceptSocketAsync();
                Interlocked.Increment(ref _conexoes);
                socket.LingerState = new LingerOption(enable: true, seconds: 0); // fecha com RST
                socket.Close();
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or SocketException)
        {
            // listener parado no Dispose
        }
    }

    public void Dispose() => _listener.Stop();
}
