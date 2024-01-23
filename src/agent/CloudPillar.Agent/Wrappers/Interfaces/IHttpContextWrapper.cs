namespace CloudPillar.Agent.Wrappers;

public interface IHttpContextWrapper
{
    string GetDisplayUrl(HttpContext httpRequest);
    Endpoint? GetEndpoint(HttpContext httpContext);
    bool TryGetValue(IDictionary<string, Microsoft.Extensions.Primitives.StringValues> headers, string key, out Microsoft.Extensions.Primitives.StringValues value);
}