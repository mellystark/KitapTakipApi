using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Dtos;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifre 6-100 karakter arasında olmalıdır.")]
    public string NewPassword { get; set; } = string.Empty;
}