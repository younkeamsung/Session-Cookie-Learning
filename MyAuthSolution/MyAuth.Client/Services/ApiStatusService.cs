using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyAuth.Client.Services
{
    public class ApiStatusService(HttpClient httpClient)
    {
        private const string ApiBaseUrl = "https://localhost:7150";

        public async Task<ApiStatusResponse?> GetStatusAsync()
        {
            return await httpClient.GetFromJsonAsync<ApiStatusResponse>($"{ApiBaseUrl}/api/status");
        }

        public async Task<AuthStatusResponse?> GetAuthStatusAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/api/auth/me");

            // 브라우저가 MVC 인증 쿠키를 요청에 포함하도록 설정
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            using var response = await httpClient.SendAsync(request);

            if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new AuthStatusResponse
                {
                    IsAuthenticated = false,
                    UserName = null
                };
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        }

        public async Task LogoutAsync()
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/api/auth/antiforgery");

            tokenRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            tokenRequest.SetBrowserRequestCache(BrowserRequestCache.NoStore);

            using var tokenResponseMessage = await httpClient.SendAsync(tokenRequest);

            tokenResponseMessage.EnsureSuccessStatusCode();

            var tokenResponse = await tokenResponseMessage.Content.ReadFromJsonAsync<AntiforgeryResponse>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (string.IsNullOrWhiteSpace(tokenResponse?.RequestToken))
            {
                throw new InvalidOperationException(
                    "Antiforgery 토큰을 가져오지 못했습니다.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{ApiBaseUrl}/api/auth/logout");

            request.SetBrowserRequestCredentials(
                BrowserRequestCredentials.Include);

            request.Headers.Add("X-CSRF-TOKEN", tokenResponse.RequestToken);

            using var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }
    }
    public class ApiStatusResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
    }

    public sealed class AuthStatusResponse
    {
        public bool IsAuthenticated { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
    }

    public class AntiforgeryResponse
    {
        public string? RequestToken { get; set; }
    }
}


