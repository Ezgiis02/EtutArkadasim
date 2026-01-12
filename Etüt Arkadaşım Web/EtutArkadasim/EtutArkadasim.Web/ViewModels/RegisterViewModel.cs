using System.ComponentModel.DataAnnotations;

// Bu dosyayı "ViewModels" klasörüne ekleyin.
namespace EtutArkadasim.Web.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        
        [Display(Name = "Bölüm")]
        public string? DepartmentId { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter olmalıdır.")]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre (Tekrar) alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")] // Password alanı ile karşılaştır
        [Display(Name = "Şifre (Tekrar)")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
