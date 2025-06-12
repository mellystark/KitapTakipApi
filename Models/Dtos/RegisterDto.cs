using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Dtos;

public class RegisterDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Kullanıcı adı 50 karakterden uzun olamaz.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre 6-100 karakter arasında olmalıdır.")]
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}