using Microsoft.AspNetCore.Mvc;
using MAI.API.Data;
using MAI.API.Models;

namespace MAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TestController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "MAI API is working! 🚀", timestamp = DateTime.UtcNow });
        }

        [HttpGet("database")]
        public IActionResult TestDatabase()
        {
            try
            {
                // Проверяем подключение к БД
                var canConnect = _db.Database.CanConnect();
                var userCount = _db.Users.Count();

                return Ok(new
                {
                    message = "Database connected! ✅",
                    canConnect = canConnect,
                    usersInDatabase = userCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create-test-user")]
        public IActionResult CreateTestUser()
        {
            try
            {
                var user = new User
                {
                    Email = "test@mai.app",
                    PasswordHash = "test_hash",
                    Tier = SubscriptionTier.Free
                };

                _db.Users.Add(user);
                _db.SaveChanges();

                return Ok(new { message = "Test user created! ✅", user = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}