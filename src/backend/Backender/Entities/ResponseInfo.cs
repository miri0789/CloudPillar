using System;
using System.Collections.Generic;
using System.Net;

namespace Backender.Entities;
public record ResponseInfo
{
    public HttpStatusCode? StatusCode { get; set; }
    public string? Response { get; set; }
    public IDictionary<string, string>? ResponseHeaders { get; set; }
    public IDictionary<string, string>? RequestHeaders { get; set; }
}
