using System.ComponentModel.DataAnnotations;

namespace KitapTakipApi.Models.Dtos
{
	public class UpdateProfileDto
	{
		[Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
		[StringLength(50, ErrorMessage = "Kullanıcı adı 50 karakterden uzun olamaz.")]
		public string UserName { get; set; } = string.Empty;

		[Required(ErrorMessage = "E-posta zorunludur.")]
		[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
		public string Email { get; set; } = string.Empty;
	}
}
