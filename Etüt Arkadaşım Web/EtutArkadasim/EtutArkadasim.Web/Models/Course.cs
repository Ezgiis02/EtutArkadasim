using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EtutArkadasim.Models
{
    public class Course
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("courseName")]
        public string CourseName { get; set; } = string.Empty;

        [BsonElement("department")]
        public string Department { get; set; } = string.Empty; // Örn: Bilgisayar Mühendisliği

        [BsonElement("courseCode")]
        public string CourseCode { get; set; } = string.Empty; // Örn: CENG301
    }
}