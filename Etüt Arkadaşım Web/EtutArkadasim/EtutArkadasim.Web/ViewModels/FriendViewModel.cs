namespace EtutArkadasim.Web.ViewModels
{
    public class FriendViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        // YENİ: Bölüm Adı
        public string DepartmentName { get; set; } = string.Empty;
    }
}