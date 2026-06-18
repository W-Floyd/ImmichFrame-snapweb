using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;

public class CustomAuthenticationMiddleware(RequestDelegate next, IGeneralSettings settings)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!string.IsNullOrEmpty(settings.OidcAuthority))
        {
            var oidcResult = await context.AuthenticateAsync("OidcBearer");
            if (oidcResult.Succeeded)
            {
                context.User = oidcResult.Principal!;
                await next(context);
                return;
            }
        }

        var result = await context.AuthenticateAsync("ImmichFrameScheme");

        if (!result.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(result.Failure?.Message ?? "Unauthorized");
            return;
        }

        await next(context);
    }
}
