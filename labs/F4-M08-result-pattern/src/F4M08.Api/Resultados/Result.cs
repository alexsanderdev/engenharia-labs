namespace F4M08.Api.Resultados;

/// <summary>
/// Resultado de uma operação que pode falhar por motivo de NEGÓCIO.
/// Invariante: sucesso ⇔ <see cref="Error"/> == <see cref="Error.None"/>.
/// Falhas técnicas inesperadas (banco fora, bug) continuam sendo exceções.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (isSuccess && error != Error.None)
            throw new ArgumentException("Um resultado de sucesso não pode carregar erro.", nameof(error));
        if (!isSuccess && error == Error.None)
            throw new ArgumentException("Um resultado de falha precisa de um erro (não use Error.None).", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Permite <c>return PedidoErrors.NaoEncontrado(id);</c> num método que devolve <see cref="Result"/>.</summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>Executa um dos dois ramos e devolve o que ele produzir.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}

/// <summary>Resultado com valor. <see cref="Value"/> só pode ser lido em caso de sucesso.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>O valor do sucesso. Em falha, lança <see cref="InvalidOperationException"/> (leia <see cref="Result.IsSuccess"/> antes).</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Não há valor em um resultado de falha ({Error.Code}).");

#pragma warning disable CA1000 // fábricas estáticas no tipo genérico são a API idiomática de um Result
    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
#pragma warning restore CA1000

    /// <summary>Permite <c>return pedido;</c> num método que devolve <c>Result&lt;Pedido&gt;</c>.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Permite <c>return PedidoErrors.NaoEncontrado(id);</c> num método que devolve <c>Result&lt;T&gt;</c>.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Transforma o valor em caso de sucesso; em falha, propaga o MESMO erro sem chamar <paramref name="map"/>.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Error);
    }

    /// <summary>Encadeia outra operação que também pode falhar; para no primeiro erro.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? bind(Value) : Result<TOut>.Failure(Error);
    }

    /// <summary>Executa um dos dois ramos e devolve o que ele produzir.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
