using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyAuth.Mvc.Data;
using MyAuth.Mvc.Models;
using MyAuth.Mvc.Services;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Claims;

namespace MyAuth.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PasswordService _passwordService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext db, PasswordService passwordService, ILogger<AccountController> logger)
        {
            _db = db;
            _passwordService = passwordService;
            _logger = logger;
        }

        private async Task RecordFailedLoginAsync(int userId)
        {
            const int maxAttempts = 3;

            for(var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var user = await _db.Users
                    .SingleOrDefaultAsync(u => u.Id == userId);

                if (user is null)
                    return;

                var now = DateTime.UtcNow;

                // 다른 요청이 이미 계정을 잠갔다면 추가 처리하지 않음
                if(user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > now)
                {
                    return;
                }

                // 잠금 시간이 지났다면 실패 횟수를 새로 계산
                if(user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value <= now)
                {
                    user.FailedLoginAttempts = 0;
                    user.LockoutEndUtc = null;
                }

                user.FailedLoginAttempts++;

                if(user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEndUtc = now.AddMinutes(15);

                    _logger.LogWarning("계정 잠금 적용. 사용자 ID: {UserId}", user.Id);
                }

                // EF Core가 다음 업데이트에서 동시성 변경을 감지하도록 갱신
                user.ConcurrencyStamp = Guid.NewGuid();

                try
                {
                    await _db.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // 충돌한 엔티티 상태를 DB의 최신 값으로 다시 읽음
                    foreach(var entry in _db.ChangeTracker.Entries<AppUser>())
                    {
                        if(entry.Entity.Id == userId)
                        {
                            await entry.ReloadAsync();
                            break;
                        }
                    }
                }
            }

            throw new InvalidOperationException("로그인 실패 횟수를 안전하게 기록하지 못했습니다. 다시 시도해 주세요.");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            model.UserName = model.UserName.Trim();
            model.Email = model.Email.Trim();

            var userNameExists = await _db.Users
                .AnyAsync(u => u.UserName == model.UserName);

            if (userNameExists)
            {
                ModelState.AddModelError(nameof(model.UserName), "이미 사용 중인 아이디입니다.");

                return View(model);
            }

            var emailExists = await _db.Users
            .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "이미 사용 중인 이메일입니다.");

                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                Role = "User",
                CreatedAtUtc = DateTime.UtcNow
            };

            user.PasswordHash = _passwordService.HashPassword(
            user,
            model.Password);

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
                when (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062)
            {
                ModelState.AddModelError(string.Empty, "이미 사용 중인 아이디 또는 이메일입니다.");
                return View(model);
            }


            return RedirectToAction(nameof(RegisterSuccess));
        }

        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [EnableRateLimiting("LoginLimit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _db.Users
                .SingleOrDefaultAsync(u => u.UserName == model.UserName);

            var now = DateTime.UtcNow;

            // 계정이 잠겨 있는지 확인
            if(user is not null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > now)
            {
                ModelState.AddModelError(string.Empty, "계정이 잠겨 있습니다. 15분 후에 시도해주세요.");

                return View(model);
            }

            // 이전 잠금 시간이 만료되었다면 실패 횟수 초기화
            if(user is not null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value <= now)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = null;

                await _db.SaveChangesAsync();
            }

            // 사용자 조회 또는 비밀번호 검증 실패
            if (user is null ||
                !_passwordService.VerifyPassword(user, model.Password, user.PasswordHash))
            {
                var safeUserName = (model.UserName ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

                if(safeUserName.Length > 50)
                {
                    safeUserName = safeUserName[..50];
                }

                _logger.LogWarning("로그인 실패. 사용자 이름: {UserName}", safeUserName);

                // 실제 사용자가 존재할 때만 실패 횟수 증가
                if(user is not null)
                {
                    await RecordFailedLoginAsync(user.Id);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "아이디 또는 비밀번호가 올바르지 않습니다.");

                return View(model);
            }

            // 로그인 성공 시 실패 횟수 초기화
            if(user.FailedLoginAttempts != 0 || user.LockoutEndUtc.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = null;

                await _db.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                // 최초 로그인 시각: 슬라이딩 갱신으로 변경되지 않음.
                new("AuthSessionStartedUtc", DateTimeOffset.UtcNow.ToString("O"))
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }
        // 로그인이 필요한 페이지
        [Authorize]
        [HttpGet]
        public IActionResult MyPage()
        {
            return View();
        }
        // 관리자 전용 페이지
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Admin()
        {
            return View();
        }
        // 관리자전용 페이지접근 실패페이지
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
