using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = string.Empty;
}