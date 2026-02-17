using Microsoft.EntityFrameworkCore;
using MAI.API.Data;
using MAI.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(connectionString))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    var password = Uri.UnescapeDataString(userInfo[1]);
    
    // Порт по умолчанию 5432 если не указан
    var port = uri.Port > 0 ? uri.Port : 5432;

    connectionString = $"Host={uri.Host};" +
                      $"Port={port};" +
                      $"Database={uri.AbsolutePath.TrimStart('/')};" +
                      $"Username={userInfo[0]};" +
                      $"Password={password};" +
                      $"SSL Mode=Require;" +
                      $"Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseCors("AllowAll");

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Swagger всегда включён
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MAI API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();

// Root endpoint ← ДОБАВИЛИ!
app.MapGet("/", () => Results.Ok(new
{
    status = "MAI Backend is running! 🚀",
    version = "1.0",
    swagger = "/swagger",
    timestamp = DateTime.UtcNow
}));

app.Run();