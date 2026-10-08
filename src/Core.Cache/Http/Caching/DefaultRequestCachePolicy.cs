using Core.Cache.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Core.Cache.Http.Caching;

/// <summary>
/// Default policy deciding whether an incoming request may be served from, or
/// written to, the HTTP response cache.
/// </summary>
/// <remarks>
/// The implementation is public but the contract it satisfies,
/// <c>IRequestCachePolicy</c>, is internal, so it cannot currently be substituted
/// from outside the assembly.
/// </remarks>
public sealed class DefaultRequestCachePolicy
    : IRequestCachePolicy
{
    /// <summary>
    /// Determines whether the request is eligible for response caching.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>
    /// <see langword="true"/> for <c>GET</c> and <c>HEAD</c> requests that carry
    /// no <c>Authorization</c> header; otherwise <see langword="false"/>.
    /// </returns>
    public bool CanCache(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        if (context.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return false;
        }

        return true;
    }
}