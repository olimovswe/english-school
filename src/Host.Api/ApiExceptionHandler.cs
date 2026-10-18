using Microsoft.AspNetCore.Diagnostics;

namespace EnglishSchool.Host.Api;

/// <summary>
/// Endpointlar ushlamagan xatolar uchun yagona javob: RFC 9457 ProblemDetails, oʻzbekcha matn va <c>traceId</c>.
/// Istisno tafsilotlari mijozga koʻrsatilmaydi, logga yoziladi.
/// </summary>
internal sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // Mijoz ulanishni uzdi: javobni oladigan hech kim yoʻq, bu server xatosi ham emas.
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var (status, title, detail) = exception is BadHttpRequestException badRequest
            ? (badRequest.StatusCode, "Notoʻgʻri soʻrov", "Soʻrovni oʻqib boʻlmadi: tana formati va hajmini tekshiring.")
            : (StatusCodes.Status500InternalServerError, "Server xatosi", "Kutilmagan xatolik yuz berdi. Birozdan keyin qayta urinib koʻring.");

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(exception, httpContext.Request.Method, httpContext.Request.Path.Value);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Method} {Path}: ushlanmagan xatolik.")]
    private partial void LogUnhandled(Exception exception, string method, string? path);
}
