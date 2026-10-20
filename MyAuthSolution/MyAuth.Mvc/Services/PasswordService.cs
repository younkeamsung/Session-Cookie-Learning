using Microsoft.AspNetCore.Identity;
using MyAuth.Mvc.Models;

namespace MyAuth.Mvc.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<AppUser> _hasher = new();

        // 새 비밀번호 해시 생성
        public string HashPassword(AppUser user, string password)
        {
            return _hasher.HashPassword(user, password);
        }

        // 입력한 비밀번호 기존해시와 비교 검증
        public bool VerifyPassword(AppUser user, string password, string passwordHash)
        {
            var result = _hasher.VerifyHashedPassword(user, passwordHash, password);

            return result != PasswordVerificationResult.Failed;
        }
    }
}
