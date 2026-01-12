using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EtutArkadasim.Web.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        // --- EKSİK OLABİLECEK ALANLAR (Bunlar olmazsa kayıt çalışmaz) ---

        [Display(Name = "Şehir")]
        public string? City { get; set; }

        [Display(Name = "İlçe")]
        public string? District { get; set; }

        [Display(Name = "Tercih Edilen Mekan")]
        public string? PreferredLocationsText { get; set; }

        public string? DepartmentId { get; set; }

        public string? CurrentProfileImageUrl { get; set; }

        // Profil resmi yüklemek için bu alan ŞARTTIR
        [Display(Name = "Profil Resmi")]
        public IFormFile? ProfileImageFile { get; set; }

        // Şifre değiştirme alanları
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string? ConfirmNewPassword { get; set; }
    }
}