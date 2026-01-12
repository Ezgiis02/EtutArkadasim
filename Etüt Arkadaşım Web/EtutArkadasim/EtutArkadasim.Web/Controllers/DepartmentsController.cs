using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EtutArkadasim.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly IMongoCollection<Department> _departmentsCollection;

        public DepartmentsController(IMongoDatabase database)
        {
            // Veritabanındaki "departments" koleksiyonuna bağlanıyoruz
            _departmentsCollection = database.GetCollection<Department>("departments");
        }

        // --- TÜM BÖLÜMLERİ GETİR ---
        // Rota: GET /api/departments
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _departmentsCollection.Find(_ => true)
                                .SortBy(d => d.DepartmentName) // Alfabetik sırala
                                .ToListAsync();
            return Ok(departments);
        }

        // --- TEK BİR BÖLÜM GETİR ---
        // Rota: GET /api/departments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var department = await _departmentsCollection.Find(d => d.Id == id).FirstOrDefaultAsync();
            if (department == null) return NotFound("Bölüm bulunamadı.");
            return Ok(department);
        }

        // --- ÖRNEK BÖLÜMLERİ EKLE (SEED) ---
        // Veritabanı boşsa başlangıç verilerini doldurur
        [HttpGet("seed")]
        public async Task<IActionResult> SeedDepartments()
        {
            var count = await _departmentsCollection.CountDocumentsAsync(_ => true);
            if (count > 0) return BadRequest("Bölümler zaten mevcut.");

            var departments = new List<Department>
            {
                new Department { DepartmentName = "Bilgisayar Mühendisliği", DepartmentCode = "CENG" },
                new Department { DepartmentName = "Elektrik-Elektronik Mühendisliği", DepartmentCode = "EE" },
                new Department { DepartmentName = "Makine Mühendisliği", DepartmentCode = "ME" },
                new Department { DepartmentName = "Endüstri Mühendisliği", DepartmentCode = "IE" },
                new Department { DepartmentName = "Yazılım Mühendisliği", DepartmentCode = "SENG" },
                new Department { DepartmentName = "Psikoloji", DepartmentCode = "PSYC" },
                new Department { DepartmentName = "Hukuk", DepartmentCode = "LAW" }
            };

            await _departmentsCollection.InsertManyAsync(departments);
            return Ok($"{departments.Count} adet bölüm eklendi.");
        }
    }
}