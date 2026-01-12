using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EtutArkadasim.Web.Models
{
    public class Department
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // MongoDB'ye "DepartmentName" olarak girdiğin için burayı güncelledik
        [BsonElement("DepartmentName")]
        public string DepartmentName { get; set; } = string.Empty;

        // Diğer alanları da JSON yapına göre büyük harfle başlatmak 
        // ileride manuel veri girerken hata almamanı sağlar
        [BsonElement("DepartmentCode")]
        public string DepartmentCode { get; set; } = string.Empty;

        [BsonElement("Faculty")]
        public string Faculty { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("CreatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}