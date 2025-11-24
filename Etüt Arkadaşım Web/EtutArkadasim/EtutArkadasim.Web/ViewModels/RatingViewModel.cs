using System.ComponentModel.DataAnnotations;

namespace EtutArkadasim.Web.ViewModels
{
    public class RatingViewModel
    {
        // Puan verdiğimiz kullanıcının (arkadaşımızın) Id'si
        [Required]
        public string RatedUserId { get; set; } = string.Empty;

        // 1-5 arası verdiğimiz puan
        [Required]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Score { get; set; }
    }
}
