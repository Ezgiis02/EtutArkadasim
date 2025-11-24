using System.ComponentModel.DataAnnotations;

namespace EtutArkadasim.Web.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Profil Fotoğrafı URL'si")]
        [Url(ErrorMessage = "Lütfen geçerli bir URL girin (örn: https://...)")]
        public string ProfileImageUrl { get; set; } = string.Empty;
    }
}