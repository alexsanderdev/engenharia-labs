namespace F4M08.Api.Resultados;

/// <summary>
/// Resultado de uma operação que pode falhar por motivo de NEGÓCIO.
/// Invariante: sucesso ⇔ <see cref="Error"/> == <see cref="Error.None"/>.
/// Falhas técnicas inesperadas (banco fora, bug) continuam sendo exceções.
/// </summary>
public class Result
{
    /// <summary>
    /// Garante a invariante: sucesso com erro ou falha com <see cref="Error.None"/> → <see cref="ArgumentException"/>.
    /// </summary>
    protected Result(bool isSuccess, Error error) =>
        throw new NotImplementedException(
            "TODO: valide a invariante (sucesso ⇔ Error.None; senão ArgumentException) e guarde IsSuccess e Error.");

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; } = Error.None;

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Permite <c>return PedidoErrors.NaoEncontrado(id);</c> num método que devolve <see cref="Result"/>.</summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>Executa um dos dois ramos e devolve o que ele produzir.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        throw new NotImplementedException("TODO: sucesso → onSuccess(); falha → onFailure(Error).");
}

/// <summary>Resultado com valor. <see cref="Value"/> só pode ser lido em caso de sucesso.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>O valor do sucesso. Em falha, lança <see cref="InvalidOperationException"/> com o código do erro na mensagem.</summary>
    public T Value => throw new NotImplementedException(
        "TODO: sucesso → _value; falha → InvalidOperationException mencionando Error.Code.");

#pragma warning disable CA1000 // fábricas estáticas no tipo genérico são a API idiomática de um Result
    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
#pragma warning restore CA1000

    /// <summary>Permite <c>return pedido;</c> num método que devolve <c>Result&lt;Pedido&gt;</c>.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Permite <c>return PedidoErrors.NaoEncontrado(id);</c> num método que devolve <c>Result&lt;T&gt;</c>.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Transforma o valor em caso de sucesso; em falha, propaga o MESMO erro sem chamar <paramref name="map"/>.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        throw new NotImplementedException("TODO: sucesso → Result<TOut>.Success(map(Value)); falha → Result<TOut>.Failure(Error).");

    /// <summary>Encadeia outra operação que também pode falhar; para no primeiro erro.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        throw new NotImplementedException("TODO: sucesso → bind(Value); falha → Result<TOut>.Failure(Error).");

    /// <summary>Executa um dos dois ramos e devolve o que ele produzir.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        throw new NotImplementedException("TODO: sucesso → onSuccess(Value); falha → onFailure(Error).");
}
