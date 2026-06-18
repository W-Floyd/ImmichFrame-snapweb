using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IGeneralSettings settings) : ControllerBase
{
    [HttpGet("oidc")]
    public IActionResult GetOidcConfig()
    {
        if (string.IsNullOrEmpty(settings.OidcAuthority))
            return Ok(new { enabled = false });

        return Ok(new
        {
            enabled = true,
            authority = settings.OidcAuthority,
            clientId = settings.OidcClientId ?? string.Empty,
            scopes = string.IsNullOrEmpty(settings.OidcScopes) ? "openid profile" : settings.OidcScopes,
            protectFrame = settings.OidcProtectFrame,
        });
    }
}
