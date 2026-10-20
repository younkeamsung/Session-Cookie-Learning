using Microsoft.AspNetCore.Mvc;
using MyAuth.Mvc.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace MyAuth.Mvc.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult ClaimsDemo()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "kim"),
                new Claim(ClaimTypes.Email, "kim@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Demo");

            var principal = new ClaimsPrincipal(identity);

            return Content(
                $"인증 여부: {principal.Identity?.IsAuthenticated}\n" +
                $"이름: {principal.Identity?.Name}\n" +
                $"Claim 개수: {principal.Claims.Count()}");
        }
        public IActionResult Index()
        {
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
            ViewBag.UserName = User.Identity?.Name ?? "(로그인하지 않음)";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
    }
}
