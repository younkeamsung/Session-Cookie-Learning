using System.ComponentModel.DataAnnotations;

namespace MyAuth.Mvc.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "아이디를 입력해 주세요.")]
        [StringLength(30, MinimumLength = 5,
        ErrorMessage = "아이디는 5 ~ 30자여야 합니다.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "이메일을 입력해 주세요.")]
        [EmailAddress(ErrorMessage = "올바른 이메일 형식이 아닙니다.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
        [StringLength(100, MinimumLength = 10,
            ErrorMessage = "비밀번호는 최소 10자여야 합니다.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "비밀번호 확인을 입력해 주세요.")]
        [Compare(nameof(Password),
            ErrorMessage = "비밀번호가 일치하지 않습니다.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
