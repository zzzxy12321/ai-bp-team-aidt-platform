using AIDT.Domain.Common;
using Microsoft.AspNetCore.Antiforgery;

namespace AIDT.Api.Middleware;

public sealed class CsrfProtectionMiddleware(
    RequestDelegate next,
    IAntiforgery antiforgery)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                throw new ValidationException("CSRF 令牌缺失或无效，请刷新页面后重试。");
            }
        }

        await next(context);
    }
}
