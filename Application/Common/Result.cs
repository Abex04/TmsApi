namespace TmsApi.Application.Common;

// A typed result container that forces callers to handle both success and failure.
// The compiler makes it impossible to access .Value on a failure or .Error on
// a success — you must use .Match(onSuccess, onFailure) to unwrap safely.
// readonly record struct = stack-allocated, immutable, value-equality for free.
public readonly record struct Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess { get; }

    // Private constructors — callers must use Success() or Failure() factories
    private Result(TValue value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(TError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    // Factory methods — the only way to create a Result
    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    // Accessing .Value on a failure throws — use .Match() instead
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result is failure; call Match instead of Value.");

    // Accessing .Error on a success throws — use .Match() instead
    public TError Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Result is success; call Match instead of Error.");

    // The safe way to unwrap — forces you to handle both cases
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<TError, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}