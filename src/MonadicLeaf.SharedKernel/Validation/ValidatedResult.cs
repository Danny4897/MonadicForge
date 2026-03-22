using System.Text.Json;
using MonadicSharp;

namespace MonadicLeaf.SharedKernel.Validation;

/// <summary>
/// Lightweight ValidatedResult&lt;T&gt; — parses JSON and accumulates validation errors.
/// Mirrors the MonadicSharp.AI API so the code can be swapped when the workspace
/// MonadicSharp copy includes MonadicSharp.AI.
/// </summary>
public sealed class ValidatedResult<T>
{
    private readonly T? _value;
    private readonly List<string> _errors;
    private readonly bool _parsedOk;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private ValidatedResult(T value)
    {
        _value = value;
        _parsedOk = true;
        _errors = [];
    }

    private ValidatedResult(string parseError)
    {
        _parsedOk = false;
        _errors = [parseError];
    }

    internal static ValidatedResult<T> FromValue(T value) => new(value);
    internal static ValidatedResult<T> FromParseError(string error) => new(error);

    /// <summary>Adds a validation error if the predicate fails. No-ops on a failed parse.</summary>
    public ValidatedResult<T> Validate(Func<T, bool> predicate, string errorMessage)
    {
        if (_parsedOk && !predicate(_value!))
            _errors.Add(errorMessage);
        return this;
    }

    public Result<T> AsResult() =>
        _errors.Count == 0
            ? Result<T>.Success(_value!)
            : Result<T>.Failure(Error.Validation(string.Join("; ", _errors)));

    public Task<Result<T>> AsResultAsync() => Task.FromResult(AsResult());

    /// <summary>Deserializes JSON into T. Any exception becomes a Failure.</summary>
    public static ValidatedResult<T> Parse(string json)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(json, JsonOpts);
            return value is null
                ? FromParseError("Deserialized value is null")
                : FromValue(value);
        }
        catch (Exception ex)
        {
            return FromParseError($"JSON parse failed: {ex.Message}");
        }
    }
}

public static class ValidatedResultExtensions
{
    public static ValidatedResult<T> ParseAs<T>(this string json) =>
        ValidatedResult<T>.Parse(json);

    /// <summary>Propagates Result&lt;string&gt; failures through to ValidatedResult.</summary>
    public static ValidatedResult<T> ParseAs<T>(this Result<string> result)
    {
        if (result.IsFailure)
            return ValidatedResult<T>.FromParseError(result.Error.Message);
        return ValidatedResult<T>.Parse(result.Value);
    }

    public static async Task<ValidatedResult<T>> ParseAs<T>(this Task<Result<string>> task)
    {
        var result = await task;
        return result.ParseAs<T>();
    }
}
