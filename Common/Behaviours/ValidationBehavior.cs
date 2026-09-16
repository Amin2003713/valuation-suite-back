using ValidationException = FluentValidation.ValidationException;

namespace Common.Behaviours;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IList<IValidator<TRequest>> _validators =
        validators?.ToList() ?? throw new ArgumentNullException(nameof(validators));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // if there are no validators, short-circuit immediately
        if (_validators.Count == 0)
            return await next(cancellationToken);

        // build the FluentValidation context once
        var context = new ValidationContext<TRequest>(request);

        // run all validations in parallel
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        // collect all failures
        var errors = results.SelectMany(r => r.Errors).Where(e => e != null).ToList();

        if (errors.Count != 0)
            throw new ValidationException(errors);

        return await next(cancellationToken);
    }
}
