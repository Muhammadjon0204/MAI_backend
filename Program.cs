using Microsoft.EntityFrameworkCore;
using MAI.API.Data;
using MAI.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - один раз!
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Gemini AI сервис
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();

// База данных - поддержка и localhost и Render!
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(connectionString))
{
   
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':',2);
    var password = Uri.UnescapeDataString(userInfo[1]); 

    connectionString = $"Host={uri.Host};" +
                      $"Port={uri.Port};" +
                      $"Database={uri.AbsolutePath.TrimStart('/')};" +
                      $"Username={userInfo[0]};" +
                      $"Password={password};" +
                      $"SSL Mode=Require;" +
                      $"Trust Server Certificate=true";
}
else
{
    // Локальный PostgreSQL
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// CORS должен быть первым!
app.UseCors("AllowAll");

// Auto migrate при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseAuthorization();
app.MapControllers();

app.Run();