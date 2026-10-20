using System.ComponentModel.DataAnnotations;

namespace MyAuth.Mvc.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "아이디를 입력해 주세요.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
