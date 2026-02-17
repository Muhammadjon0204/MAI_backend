using Microsoft.AspNetCore.Mvc;
using MAI.API.Services;

namespace MAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MathController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly ILogger<MathController> _logger;

        public MathController(GeminiService geminiService, ILogger<MathController> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
            _logger.LogInformation("MathController initialized");
        }

        [HttpPost("solve")]
        public async Task<IActionResult> SolveProblem([FromBody] MathRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Problem))
                {
                    return BadRequest(new { error = "Задача не может быть пустой" });
                }

                _logger.LogInformation($"Solving problem: {request.Problem}");

                var solution = await _geminiService.SolveMathProblem(request.Problem);

                return Ok(new
                {
                    problem = request.Problem,
                    solution = solution,
                    solver = "Gemini AI",
                    timestamp = DateTime.UtcNow,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error solving problem: {ex.Message}");
                return StatusCode(500, new { 
                    error = "Ошибка при решении задачи", 
                    details = ex.Message,
                    success = false,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint called");
            return Ok(new { 
                message = "Math Controller is working! 🧮",
                timestamp = DateTime.UtcNow,
                version = "1.0",
                endpoints = new[] { "POST /api/math/solve", "GET /api/math/test", "GET /api/math/models" }
            });
        }

        [HttpGet("models")]
        public async Task<IActionResult> GetAvailableModels()
        {
            try
            {
                _logger.LogInformation("Getting available Gemini models");
                
                var models = await _geminiService.GetAvailableModels();
                
                _logger.LogInformation($"Found {models.Count} available models");
                
                return Ok(new 
                { 
                    success = true,
                    availableModels = models,
                    count = models.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting models: {ex.Message}");
                return StatusCode(500, new { 
                    error = "Ошибка при получении списка моделей", 
                    details = ex.Message,
                    success = false,
                    timestamp = DateTime.UtcNow,
                    note = "Возможно, API ключ неверный или отсутствует доступ к Gemini API"
                });
            }
        }
        
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
                
                return Ok(new
                {
                    status = "healthy",
                    service = "MAI Math API",
                    version = version,
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    machine = Environment.MachineName,
                    endpoints = new[]
                    {
                        new { method = "POST", path = "/api/math/solve", description = "Решить математическую задачу" },
                        new { method = "GET", path = "/api/math/test", description = "Тестовый эндпоинт" },
                        new { method = "GET", path = "/api/math/models", description = "Получить список доступных моделей Gemini" },
                        new { method = "GET", path = "/api/math/health", description = "Проверка здоровья сервиса" }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Health check failed: {ex.Message}");
                return StatusCode(503, new { status = "unhealthy", error = ex.Message });
            }
        }
        
        [HttpPost("test-simple")]
        public async Task<IActionResult> TestSimple([FromBody] SimpleMathRequest request)
        {
            try
            {
                _logger.LogInformation($"Testing simple problem: {request.Expression}");
                
                await Task.Delay(10);
                
                // Простая логика для тестирования, если Gemini недоступен
                var testSolution = $"Тестовый расчет для: {request.Expression}\n" +
                                  "В этом режиме используется простая логика, так как Gemini API временно недоступен.\n" +
                                  "Для реальных решений настройте API ключ Gemini.";
                
                return Ok(new
                {
                    problem = request.Expression,
                    solution = testSolution,
                    mode = "test",
                    timestamp = DateTime.UtcNow,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in test-simple: {ex.Message}");
                return StatusCode(500, new { error = "Тестовая ошибка", details = ex.Message });
            }
        }
    }

    public class MathRequest
    {
        public string Problem { get; set; } = string.Empty;
    }
    
    public class SimpleMathRequest
    {
        public string Expression { get; set; } = string.Empty;
    }
}