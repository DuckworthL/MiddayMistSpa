using System.Net;

namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Routes HTTP requests through the ASP.NET Core middleware pipeline in-process,
/// bypassing the network. Used in production to avoid HTTP loopback issues on
/// MonsterASP.NET shared hosting where self-referencing HTTP calls fail.
/// </summary>
public sealed class InProcessHandler : HttpMessageHandler
{
    private volatile RequestDelegate? _pipeline;
    private IServiceScopeFactory? _scopeFactory;

    internal void Configure(RequestDelegate pipeline, IServiceScopeFactory scopeFactory)
    {
        _pipeline = pipeline;
        _scopeFactory = scopeFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var pipeline = _pipeline
            ?? throw new InvalidOperationException("InProcessHandler: pipeline not configured.");
        var scopeFactory = _scopeFactory!;

        // Each in-process call gets its own DI scope (same as a real HTTP request),
        // so parallel calls (Task.WhenAll) are safe — no shared DbContext.
        using var scope = scopeFactory.CreateScope();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        // Map request line
        httpContext.Request.Method = request.Method.Method;
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = request.RequestUri?.AbsolutePath ?? "/";
        httpContext.Request.QueryString = new QueryString(request.RequestUri?.Query ?? "");

        // Copy request headers
        foreach (var header in request.Headers)
            httpContext.Request.Headers[header.Key] = header.Value.ToArray();

        // Copy content (body + content headers)
        if (request.Content != null)
        {
            httpContext.Request.Body = await request.Content.ReadAsStreamAsync(cancellationToken);

            if (request.Content.Headers.ContentType != null)
                httpContext.Request.ContentType = request.Content.Headers.ContentType.ToString();
            if (request.Content.Headers.ContentLength.HasValue)
                httpContext.Request.ContentLength = request.Content.Headers.ContentLength;

            foreach (var header in request.Content.Headers)
                httpContext.Request.Headers.TryAdd(header.Key, header.Value.ToArray());
        }

        // Capture response body in memory
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        try
        {
            await pipeline(httpContext);
        }
        catch (Exception)
        {
            // If the pipeline itself throws (e.g., exception handler also fails),
            // return a clean 500 so ApiClient gets a proper HTTP status.
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        // Build HttpResponseMessage from the in-memory response
        responseBody.Seek(0, SeekOrigin.Begin);
        var response = new HttpResponseMessage((HttpStatusCode)httpContext.Response.StatusCode)
        {
            Content = new StreamContent(responseBody)
        };

        // Copy response headers
        foreach (var header in httpContext.Response.Headers)
        {
            if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        return response;
    }
}
