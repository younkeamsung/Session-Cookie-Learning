using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyAuth.Client;
using MyAuth.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();        // 인증 서비스
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ApiStatusService>(); // 서버 Api 서비스
builder.Services.AddScoped<ApiAuthenticationStateProvider>(); // api인증 상태 프로바이더

builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
