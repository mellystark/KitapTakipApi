using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Dtos;

public class BookCreateDto
{
    [Required(ErrorMessage = "Kitap başlığı zorunludur.")]
    [StringLength(100, ErrorMessage = "Başlık 100 karakterden uzun olamaz.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yazar adı zorunludur.")]
    [StringLength(50, ErrorMessage = "Yazar adı 50 karakterden uzun olamaz.")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(50, ErrorMessage = "Tür 50 karakterden uzun olamaz.")]
    public string Genre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Notlar 500 karakterden uzun olamaz.")]
    public string? Notes { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama 1000 karakterden uzun olamaz.")]
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public int? PageCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsReading { get; set; }
}
