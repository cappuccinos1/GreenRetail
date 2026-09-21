namespace GreenRetail.Core.Abstractions;

public sealed record EmptyRequest;

public interface IUseCase<in TRequest, TResult>
{
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}