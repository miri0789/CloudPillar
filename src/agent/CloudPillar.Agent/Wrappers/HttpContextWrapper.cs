using Microsoft.AspNetCore.Http.Extensions;

namespace CloudPillar.Agent.Wrappers;

public class HttpContextWrapper : IHttpContextWrapper
{
    public string GetDisplayUrl(HttpContext httpRequest)
    {
        return httpRequest.Request.GetDisplayUrl();
    }

    public Endpoint? GetEndpoint(HttpContext httpContext)
    {
        return httpContext.GetEndpoint();
    }

    public bool TryGetValue(IDictionary<string, Microsoft.Extensions.Primitives.StringValues> headers, string key, out Microsoft.Extensions.Primitives.StringValues value)
    {
        return headers.TryGetValue(key, out value);
    }
}