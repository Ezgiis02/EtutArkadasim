using EtutArkadasim.Models;
using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization; // [Authorize] için
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims; // ClaimTypes için gerekli

// Bu attributelar, bu sınıfın bir API Controller olduğunu ve 
// varsayılan rotasının "api/Courses" olacağını belirtir.
[Route("api/[controller]")]
[ApiController]
[Authorize] // Bu API'daki tüm metodlara sadece giriş yapmış kullanıcılar erişebilir
public class CoursesController : ControllerBase // MVC için 'Controller' yerine 'ControllerBase' kullanırız
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Course> _coursesCollection;
    // YENİ EKLENDİ: Kullanıcı koleksiyonuna da ihtiyacımız var
    private readonly IMongoCollection<User> _usersCollection;

    public CoursesController(IMongoDatabase database)
    {
        _database = database;
        _coursesCollection = _database.GetCollection<Course>("courses");
        // YENİ EKLENDİ: Users koleksiyonunu da tanımla
        _usersCollection = _database.GetCollection<User>("users");
    }

    // --- GEÇİCİ YARDIMCI METOT: VERİTABANINA ÖRNEK DERS EKLEME ---
    // Bu metodu API'mizi test etmek için bir kerelik kullanmıştık.
    [HttpGet("addsamples")] // Rota: /api/courses/addsamples
    public async Task<IActionResult> AddSampleCourses()
    {
        // Sadece veritabanında hiç ders yoksa ekle
        var count = await _coursesCollection.CountDocumentsAsync(_ => true);
        if (count > 0)
        {
            return BadRequest("Örnek dersler zaten mevcut.");
        }

        var newCourses = new List<Course>
        {
            new Course { CourseName = "Veri Yapıları", Department = "Bilgisayar Müh.", CourseCode = "CENG201" },
            new Course { CourseName = "Diferansiyel Denklemler", Department = "Makine Müh.", CourseCode = "MATH202" },
            new Course { CourseName = "Termodinamik", Department = "Kimya Müh.", CourseCode = "CHEM301" },
            new Course { CourseName = "Devre Analizi", Department = "Elektrik-Elektronik Müh.", CourseCode = "EE201" }
        };

        await _coursesCollection.InsertManyAsync(newCourses);
        return Ok("4 adet örnek ders eklendi.");
    }

    // --- API ENDPOINT 1: TÜM DERSLERİ GETİR ---
    // Profil sayfasında "Ders Ekle" listesini doldurmak için bunu kullanacağız.
    [HttpGet("getall")] // Rota: /api/courses/getall
    public async Task<IActionResult> GetAllCourses()
    {
        // Tüm dersleri bul ve listele
        var courses = await _coursesCollection.Find(_ => true).ToListAsync();

        // Ok() metodu, 'courses' listesini otomatik olarak JSON formatına çevirir
        return Ok(courses);
    }

    // --- API ENDPOINT 2: KULLANICININ SEÇTİĞİ DERSLERİ GETİR ---
    // Bu, Adım 3.2'de eklediğimiz YENİ metottur
    [HttpGet("getmycourses")] // Rota: /api/courses/getmycourses
    public async Task<IActionResult> GetMyCourses()
    {
        // Önce giriş yapan kullanıcının kimliğini (Id) al
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // Kullanıcıyı veritabanından bul
        var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
        if (user == null || !user.SelectedCourseIds.Any())
        {
            // Kullanıcı yoksa veya hiç ders seçmemişse boş liste döndür
            return Ok(new List<Course>());
        }

        // Kullanıcının seçtiği derslerin ID'leri ile (SelectedCourseIds)
        // 'courses' koleksiyonundan eşleşen dersleri bul
        var filter = Builders<Course>.Filter.In(c => c.Id, user.SelectedCourseIds);
        var myCourses = await _coursesCollection.Find(filter).ToListAsync();

        return Ok(myCourses);
    }

    // --- API ENDPOINT 3: KULLANICIYA DERS EKLE ---
    // Bu, Adım 3.2'de eklediğimiz diğer YENİ metottur
    [HttpPost("add/{courseId}")] // Rota: /api/courses/add/{dersin_id_si}
    public async Task<IActionResult> AddCourse(string courseId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // Dersin geçerli olup olmadığını kontrol et (isteğe bağlı ama iyi bir pratik)
        var courseExists = await _coursesCollection.Find(c => c.Id == courseId).AnyAsync();
        if (!courseExists)
        {
            return NotFound("Ders bulunamadı.");
        }

        // Kullanıcının 'selectedCourseIds' dizisine bu 'courseId'yi ekle
        // $addToSet: Eğer Id zaten listede varsa, tekrar eklemez.
        var filter = Builders<User>.Filter.Eq(u => u.Id, currentUserId);
        var update = Builders<User>.Update.AddToSet(u => u.SelectedCourseIds, courseId);

        var result = await _usersCollection.UpdateOneAsync(filter, update);

        if (result.ModifiedCount > 0)
        {
            return Ok(new { message = "Ders başarıyla eklendi." });
        }
        else
        {
            // ModifiedCount 0 ise, ders zaten listedeydi.
            return Ok(new { message = "Ders zaten seçili." });
        }
    }
}