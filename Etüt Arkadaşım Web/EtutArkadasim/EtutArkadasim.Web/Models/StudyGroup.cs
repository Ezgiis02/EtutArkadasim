using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum StudyGroupType
    {
        CourseSpecific, // Ders özel
        General,        // Genel çalışma
        ExamPrep        // Sınav hazırlık
    }

    public enum GroupPrivacy
    {
        Public,   // Herkes katılabilir
        Private   // Davet ile katılınır
    }

    public class StudyGroup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("groupName")]
        public string GroupName { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("courseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CourseId { get; set; } = string.Empty; // Hangi ders için

        [BsonElement("creatorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatorId { get; set; } = string.Empty; // Grubu oluşturan kişi

        [BsonElement("memberIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> MemberIds { get; set; } = new List<string>(); // Üye ID'leri

        [BsonElement("maxMembers")]
        public int MaxMembers { get; set; } = 10; // Maksimum üye sayısı

        [BsonElement("groupType")]
        [BsonRepresentation(BsonType.String)]
        public StudyGroupType GroupType { get; set; } = StudyGroupType.CourseSpecific;

        [BsonElement("privacy")]
        [BsonRepresentation(BsonType.String)]
        public GroupPrivacy Privacy { get; set; } = GroupPrivacy.Public;

        [BsonElement("meetingSchedule")]
        public string MeetingSchedule { get; set; } = string.Empty; // Toplantı zamanlaması

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty; // Fiziksel/online konum

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
