using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MyAuth.Mvc.Data;
using MyAuth.Mvc.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 클라이언트 Cors연결
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", async policy =>
    {
        policy
            .WithOrigins("https://localhost:7179")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
// Antiforgery 헤더 이름 설정
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// 로그인 POST 요청 리미트
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginLimit", httpContext =>
    {
        var ipAddress =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,                // IP 주소별로 횟수를 계산
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                    // 최대 5회 허용
                Window = TimeSpan.FromMinutes(1),   // 제한시간 1분
                QueueLimit = 0,                     // 초과 요청을 대기열에 넣지 않음
                AutoReplenishment = true            // 시간이 지나면 자동으로 허용 횟수 복구
            });
    });
});

// MySQL 연결 문자열 확인
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection 설정이 없습니다.");

// EF Core DbContext 등록
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
});
builder.Services.AddScoped<PasswordService>();

// Cookie Authentication 등록
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // 쿠키 이름
        options.Cookie.Name = "__Host-MyAuth.Auth";
        // 도메인을 따로 지정하지 않음
        options.Cookie.Path = "/";
        // 로그인이 필요할 때 이동할 경로
        options.LoginPath = "/Account/Login";

        // 로그인은 했지만 권한이 없을 때 이동할 경로
        options.AccessDeniedPath = "/Account/AccessDenied";

        // 브라우저 JavaScript에서 인증 쿠키에 접근하지 못하게 설정
        options.Cookie.HttpOnly = true;

        // HTTPS 연결에서만 쿠키 전송
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // 일반적인 요청 위조 위험을 줄이기 위한 기본 설정
        options.Cookie.SameSite = SameSiteMode.Lax;

        // 활동 중인 사용자의 인증 만료 시간을 갱신
        options.SlidingExpiration = true;

        // 인증 티켓의 유효 기간
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

        options.Events.OnValidatePrincipal = async context =>
        {
            const string sessionStartedClaim = "AuthSessionStartedUtc";

            const int maxSessionHours = 8;

            var value = context.Principal?
                        .FindFirst(sessionStartedClaim)?.Value;

            if (!DateTimeOffset.TryParse(value, out var sessionStartedUtc))
            {
                // 시작 시각이 없거나 잘못된 쿠키는 신뢰하지 않음
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var elapsed = DateTimeOffset.UtcNow - sessionStartedUtc;

            if (elapsed >= TimeSpan.FromHours(maxSessionHours))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("BlazorClient"); // 클라이언트

app.UseRateLimiter();    // 로그인 횟수 리미트

app.UseAuthentication(); // 사용자 확인
app.UseAuthorization();  // 접근 권한 확인

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
