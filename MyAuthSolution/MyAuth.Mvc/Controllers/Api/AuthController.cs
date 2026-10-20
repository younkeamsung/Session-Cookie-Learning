using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyAuth.Mvc.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                isAuthenticated = User.Identity?.IsAuthenticated,
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                userName = User.Identity?.Name,
                role = User.FindFirstValue(ClaimTypes.Role),
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public IActionResult Admin()
        {
            Response.Headers.CacheControl = "no-store";

            return Ok(new
            {
                message = "관리자 권한 확인 성공",
                userName = User.Identity?.Name,
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        [AllowAnonymous]
        [HttpGet("antiforgery")]
        public IActionResult GetAntiforgeryToken([FromServices] IAntiforgery antiforgery)
        {
            Response.Headers.CacheControl = "no-store";

            var tokens = antiforgery.GetAndStoreTokens(HttpContext);

            return Ok(new { requestToken = tokens.RequestToken });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromServices] IAntiforgery antiforgery)
        {
            await antiforgery.ValidateRequestAsync(HttpContext);

            
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            Response.Headers.CacheControl = "no-store";

            return NoContent();
        }
    }
}
