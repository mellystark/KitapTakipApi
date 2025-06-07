using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using KitapTakipApi.Models;

namespace KitapTakipApi.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Kullanıcı adı 50 karakterden uzun olamaz.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre hash'i zorunludur.")]
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation property for Books
    public List<Book> Books { get; set; } = new List<Book>();
}