using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Wrappers;
public class RequestWrapper : IRequestWrapper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public RequestWrapper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetHeaderValue(string headerName)
    {
        return (_httpContextAccessor.HttpContext?.Request.Headers[headerName])?.ToString();
    }
}
