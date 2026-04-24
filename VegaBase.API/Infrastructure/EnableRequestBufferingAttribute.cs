using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// Resource filter that calls <see cref="Microsoft.AspNetCore.Http.HttpRequestRewindExtensions.EnableBuffering"/>
/// before model binding, making the request body seekable for subsequent reads.
/// Applied to <c>UpdateField</c> which reads the body stream twice.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class EnableRequestBufferingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
        => context.HttpContext.Request.EnableBuffering();

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
