using EtutArkadasim.Web.Models;
using System.Collections.Generic;

namespace EtutArkadasim.Web.ViewModels
{
    public class DiscoverFilters
    {
        public List<string> CourseIds { get; set; } = new List<string>();
        public List<StudyTimePreference> PreferredTimes { get; set; } = new List<StudyTimePreference>();
        public List<StudyEnvironmentPreference> PreferredEnvironments { get; set; } = new List<StudyEnvironmentPreference>();
        public List<StudyFormatPreference> PreferredFormats { get; set; } = new List<StudyFormatPreference>();
        public List<string> Cities { get; set; } = new List<string>();
        public List<string> Districts { get; set; } = new List<string>();
        public string? SearchTerm { get; set; }
    }
}


