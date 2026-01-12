using EtutArkadasim.Web.Models;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Services
{
    public class RequestService
    {
        private readonly IMongoCollection<StudyRequest> _requestsCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public RequestService(IMongoDatabase database)
        {
            _requestsCollection = database.GetCollection<StudyRequest>("studyRequests");
            _usersCollection = database.GetCollection<User>("users");
        }

        // BU FONKSİYONU HEM WEB HEM MOBİL KULLANACAK
        public async Task<(bool IsSuccess, string Message)> SendRequestAsync(string senderId, string receiverId, string courseId)
        {
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(courseId))
                return (false, "Eksik bilgi.");

            if (senderId == receiverId)
                return (false, "Kendinize istek atamazsınız.");

            // Bekleyen istek kontrolü
            var existing = await _requestsCollection.Find(r =>
                r.Status == RequestStatus.Pending && r.CourseId == courseId &&
                ((r.SenderId == senderId && r.ReceiverId == receiverId) ||
                 (r.SenderId == receiverId && r.ReceiverId == senderId))
            ).AnyAsync();

            if (existing) return (false, "Zaten bekleyen bir talep var.");

            // Arkadaşlık kontrolü
            var senderUser = await _usersCollection.Find(u => u.Id == senderId).FirstOrDefaultAsync();
            if (senderUser.FavoriteUserIds.Contains(receiverId))
                return (false, "Zaten arkadaşsınız.");

            // KAYIT
            var newRequest = new StudyRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CourseId = courseId,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            await _requestsCollection.InsertOneAsync(newRequest);
            return (true, "İstek başarıyla gönderildi.");
        }
    }
}