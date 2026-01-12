using System.Collections.Generic;
using EtutArkadasim.Web.Models;

namespace EtutArkadasim.Web.ViewModels
{
    public class DiscoverViewModel
    {
        public List<UserMatchViewModel> Users { get; set; } = new List<UserMatchViewModel>();
        public DiscoverFilters AppliedFilters { get; set; } = new DiscoverFilters();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<Course> MyCourses { get; set; } = new List<Course>(); // Eklendi
    }
}