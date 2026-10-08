using System.Text.Json;
using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;

namespace F6M05.Tests.Parte1_Retry;

/// <summary>Passo 1 — transitório × permanente (sem broker).</summary>
public sealed class ClassificadorDeErrosTests
{
    public static TheoryData<string, Exception, TipoDeErro> Casos => new()
    {
        { "erro permanente explícito", new ErroPermanenteException("cliente não existe"), TipoDeErro.Permanente },
        { "erro transitório explícito", new ErroTransitorioException("estoque fora do ar"), TipoDeErro.Transitorio },
        { "conflito de concorrência (derivada de transitório)", new ConflitoDeConcorrenciaException(Guid.NewGuid(), 3), TipoDeErro.Transitorio },
        { "saga não encontrada (fora de ordem)", new SagaNaoEncontradaException(Guid.NewGuid(), "EstoqueReservado"), TipoDeErro.Transitorio },
        { "JSON inválido", new JsonException("corpo quebrado"), TipoDeErro.Permanente },
        { "formato inválido", new FormatException("guid inválido"), TipoDeErro.Permanente },
        { "argumento inválido", new ArgumentOutOfRangeException("quantidade"), TipoDeErro.Permanente },
        { "timeout", new TimeoutException(), TipoDeErro.Transitorio },
        { "HTTP fora do ar", new HttpRequestException("503"), TipoDeErro.Transitorio },
        { "exceção desconhecida (na dúvida, repete com limite)", new InvalidOperationException("bug?"), TipoDeErro.Transitorio },
        { "agregada com uma interna permanente", new AggregateException(new JsonException()), TipoDeErro.Permanente },
        { "agregada com uma interna transitória", new AggregateException(new TimeoutException()), TipoDeErro.Transitorio },
    };

    [Theory]
    [MemberData(nameof(Casos))]
    public void Classificar_ErrosConhecidos_RetornaOTipoEsperado(string caso, Exception erro, TipoDeErro esperado)
    {
        new ClassificadorDeErros().Classificar(erro).ShouldBe(esperado, caso);
    }
}
