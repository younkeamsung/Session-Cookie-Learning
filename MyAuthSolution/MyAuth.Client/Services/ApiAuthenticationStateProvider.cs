using System.Security.Claims;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyAuth.Client.Services
{
    public class ApiAuthenticationStateProvider(ApiStatusService apiStatus) : AuthenticationStateProvider
    {
        public string? LastError { get; private set; }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var status = await apiStatus.GetAuthStatusAsync();

                // 서버 응답을 정상적으로 받았으므로 통신 오류를 초기화.
                LastError = null;

                if (status?.IsAuthenticated != true)
                {
                    return CreateAnonymousState();
                }

                var claims = new List<Claim>();

                if (!string.IsNullOrWhiteSpace(status.UserId))
                {
                    claims.Add(new Claim(
                        ClaimTypes.NameIdentifier, status.UserId));
                }

                if (!string.IsNullOrWhiteSpace(status.UserName))
                {
                    claims.Add(new Claim(
                        ClaimTypes.Name, status.UserName));
                }

                if (!string.IsNullOrWhiteSpace(status.Role))
                {
                    claims.Add(new Claim(
                        ClaimTypes.Role, status.Role));
                }

                var identity = new ClaimsIdentity(claims, authenticationType: "ApiAuthentication");

                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch(HttpRequestException)
            {
                LastError = "인증 서버에 연결할 수 없습니다. 네트워크와 서버 상태를 확인해 주세요.";

                // 서버에 연결할 수 없으면 인증된 사용자로 취급하지 않음.
                return CreateAnonymousState();
            }
        }

        public async Task RefreshAsync()
        {
            var stateTask = GetAuthenticationStateAsync();

            NotifyAuthenticationStateChanged(stateTask);

            await stateTask;
        }

        private static AuthenticationState CreateAnonymousState()
        {
            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}
