using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. MongoDB Ayarlarý (Zaten vardý)
// ... (MongoDB ayarlarýnýz burada) ...
var mongoDbSettings = builder.Configuration.GetSection("MongoDbConfig");
var connectionString = mongoDbSettings["ConnectionString"];
var databaseName = mongoDbSettings["DatabaseName"];

// 2. MongoDB Servisleri (Zaten vardý)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(connectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// 3. MVC Servisleri (Zaten vardý)
builder.Services.AddControllersWithViews();

// 4. Authentication (Kimlik Doðrulama) Servisi (Zaten vardý)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// 5. YENÝ EKLENDÝ: SWAGGER SERVÝSLERÝ
// API Explorer'ý (API Keþfi) etkinleþtirir
builder.Services.AddEndpointsApiExplorer();
// SwaggerGen servisini (JSON üreteci) ekler
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.

// YENÝ EKLENDÝ: SWAGGER UI ARAYÜZÜ
// Swagger'ý SADECE "Development" (Geliþtirme) modunda çalýþtýrýyoruz.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Swagger arayüzünün hangi JSON dosyasýný kullanacaðýný belirtir
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EtutArkadasim API V1");
        // /swagger adresini /api-docs olarak deðiþtirebilirsiniz (isteðe baðlý)
        // options.RoutePrefix = "api-docs"; 
    });
}

// Geri kalan Hata Yönetimi (Bu zaten vardý)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ÖNEMLÝ: API Controller'larýn da çalýþmasý için bu gereklidir
app.MapControllers();

app.Run();