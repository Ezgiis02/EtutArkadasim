using EtutArkadasim.Web.Models;
using System.Collections.Generic;

namespace EtutArkadasim.Web.ViewModels
{
    public class UserMatchViewModel
    {
        public User User { get; set; } = new User();
        public StudyPreferences? Preferences { get; set; }
        public StudySchedule? Schedule { get; set; }

        // Ortak ders sayýsý
        public int CommonCoursesCount { get; set; }

        // YENÝ: Sadece bu kiþiyle ortak olan derslerin tam listesi
        public List<Course> CommonCourses { get; set; } = new List<Course>();

        public double CompatibilityScore { get; set; }
        public string DepartmentName { get; set; } = "Bölüm Belirtilmemiþ";
    }
}