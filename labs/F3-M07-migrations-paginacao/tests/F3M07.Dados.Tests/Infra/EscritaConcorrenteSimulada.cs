using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F3M07.Dados.Tests.Infra;

/// <summary>
/// Já vem pronto. Simula, de forma DETERMINÍSTICA, "outro usuário salvou no meio do caminho":
/// imediatamente antes de o DbContext enviar o UPDATE, executa em OUTRA conexão um UPDATE no mesmo pedido
/// (o que troca o rowversion). Nada de Task.Delay nem "torcer" pela ordem das threads.
/// </summary>
public sealed class EscritaConcorrenteSimulada(string connectionString, int pedidoId, decimal acrescimoNoTotal, int vezes)
    : SaveChangesInterceptor
{
    private int _restantes = vezes;

    /// <summary>Quantas vezes o SaveChanges foi chamado (= tentativas de gravação).</summary>
    public int ChamadasDeSaveChanges { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ChamadasDeSaveChanges++;

        if (_restantes > 0)
        {
            _restantes--;
            await using var conexao = new SqlConnection(connectionString);
            await conexao.OpenAsync(cancellationToken);
            await using var comando = new SqlCommand("UPDATE Pedidos SET Total = Total + @Acrescimo WHERE Id = @Id", conexao);
            comando.Parameters.AddWithValue("@Acrescimo", acrescimoNoTotal);
            comando.Parameters.AddWithValue("@Id", pedidoId);
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        return result;
    }
}
