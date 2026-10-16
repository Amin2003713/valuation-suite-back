using Application.Common;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace RequestHandlers.Tools;

/// <inheritdoc/>
/// Reads the value computed by <see cref="LanguageMiddleware"/>; falls back to the
/// configured default (fa) when there is no HttpContext (seeding, tests, background).
public sealed class LanguageResolver(IHttpContextAccessor accessor, IOptions<LanguageOptions> options) : ILanguageResolver
{
    public string Lang
    {
        get
        {
            if (accessor.HttpContext?.Items[LanguageMiddleware.ItemKey] is string lang && !string.IsNullOrWhiteSpace(lang))
                return lang;
            return options.Value.Default;
        }
    }
}
