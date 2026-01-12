using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[Route("api/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly IMongoCollection<Location> _locationsCollection;

    public LocationsController(IMongoDatabase database)
    {
        _locationsCollection = database.GetCollection<Location>("locations");
    }

    [HttpGet("districts/{cityName}")]
    public async Task<IActionResult> GetDistricts(string cityName)
    {
        // Seçilen şehre ait aktif ilçeleri getir
        var districts = await _locationsCollection
            .Find(l => l.City == cityName && l.IsActive)
            .Project(l => l.District)
            .ToListAsync();

        return Ok(districts);
    }
}