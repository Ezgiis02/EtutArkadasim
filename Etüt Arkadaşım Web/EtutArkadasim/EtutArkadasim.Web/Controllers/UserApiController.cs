using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace EtutArkadasim.Web.Controllers.Api
{
    [Route("api/user")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UserApiController(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
        }

        // GET: api/user/profile?userId=...
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest(new { message = "User ID gerekli." });

            // ID'ye göre kullanıcıyı bul
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            // Flutter'ın beklediği JSON formatı: { "user": { ... } }
            return Ok(new { user = user });
        }
    }
}