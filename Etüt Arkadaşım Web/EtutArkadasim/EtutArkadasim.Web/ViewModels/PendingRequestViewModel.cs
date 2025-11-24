using System;

namespace EtutArkadasim.Web.ViewModels
{
    public class PendingRequestViewModel
    {
        // MongoDB'deki talebin Id'si (Kabul/Red için bu Id'yi kullanacağız)
        public string RequestId { get; set; } = string.Empty;

        // Talebi gönderen kullanıcının Id'si
        public string SenderId { get; set; } = string.Empty;

        // Talebi gönderen kullanıcının Adı (UI'da bunu göstereceğiz)
        public string SenderName { get; set; } = string.Empty;

        // Talep ne zaman gönderildi?
        public DateTime RequestedAt { get; set; }
    }
}