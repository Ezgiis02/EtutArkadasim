namespace EtutArkadasim.Web.ViewModels
{
    // Arkadaş listesi için (User modelinden daha hafif)
    public class FriendViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double AverageRating { get; set; }
    }
}