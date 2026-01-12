using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Bson.Serialization.Conventions;

var pack = new ConventionPack
{
    new CamelCaseElementNameConvention(), // name -> Name eþleþmesini saðlar
    new IgnoreExtraElementsConvention(true) // Fazladan alan varsa patlamasýn
};
ConventionRegistry.Register("My Convention", pack, t => true);

var builder = WebApplication.CreateBuilder(args);

// 1. MongoDB Ayarlarý
var mongoDbSettings = builder.Configuration.GetSection("MongoDbConfig");
var connectionString = mongoDbSettings["ConnectionString"];
var databaseName = mongoDbSettings["DatabaseName"];

// 2. MongoDB Servisleri
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(connectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// 3. MVC Servisleri
builder.Services.AddControllersWithViews();

// --- EKLENDÝ: CORS SERVÝSÝ (Tarayýcý Eriþim Ýzni Ýçin) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
// ---------------------------------------------------------

// 4. Authentication (Kimlik Doðrulama) Servisi
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        options.Cookie.Name = "StudyBuddy.Auth"; // Cookie'ye bir isim verelim
        options.Cookie.HttpOnly = true;          // JavaScript eriþemez (Güvenlik)

        // ÖNEMLÝ: HTTP üzerinden çalýþmasý için bu ikisi þart:
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        // --- YENÝ EKLENEN KISIM (BAÞLANGIÇ) ---
        // Eðer istek /api ile baþlýyorsa, Login sayfasýna yönlendirme!
        // Direkt 401 hatasý ver ki Flutter anlasýn.
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        // --- YENÝ EKLENEN KISIM (BÝTÝÞ) ---
    });

// 5. Swagger Servisleri
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.

// Swagger Ayarlarý
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EtutArkadasim API V1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // app.UseHsts(); // HTTP kullanacaðýmýz için bunu kapalý tutabiliriz
}

// --- ÖNEMLÝ DEÐÝÞÝKLÝK: Emülatörde 5258 portuna (HTTP) baðlanabilmek için ---
// --- HTTPS yönlendirmesini GEÇÝCÝ OLARAK kapatýyoruz. ---
// app.UseHttpsRedirection(); 
// --------------------------------------------------------------------------

app.UseStaticFiles();

app.UseRouting();

// --- EKLENDÝ: CORS MIDDLEWARE (UseRouting'den HEMEN SONRA) ---
app.UseCors("AllowAll");
// -------------------------------------------------------------

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();