using KitapTakipApi.Models;
using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Models;

public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kitap başlığı zorunludur.")]
    [StringLength(100, ErrorMessage = "Başlık 100 karakterden uzun olamaz.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yazar adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Yazar adı 50 karakterden uzun olamaz.")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(50, ErrorMessage = "Tür 50 karakterden uzun olamaz.")]
    public string Genre { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [StringLength(500, ErrorMessage = "Notlar 500 karakterden uzun olamaz.")]
    public string? Notes { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama 1000 karakterden uzun olamaz.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sayfa sayısı pozitif bir sayı olmalıdır.")]
    public int? PageCount { get; set; }

    [Required(ErrorMessage = "Kullanıcı ID'si zorunludur.")]
    public string UserId { get; set; } = string.Empty;

    // Navigation property for EF Core
    public User User { get; set; } = null!;
    [StringLength(500, ErrorMessage = "Kapak resmi yolu 500 karakterden uzun olamaz.")]
    public string? CoverImage { get; set; }
}