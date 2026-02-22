using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MAI.API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ApiKeys:Gemini"] ?? throw new Exception("Gemini API key not found");
            _logger = logger;
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            _logger.LogInformation($"Gemini Service initialized");
            _logger.LogInformation($"API Key length: {_apiKey.Length}");
            _logger.LogInformation($"API Key starts with: {_apiKey.Substring(0, Math.Min(6, _apiKey.Length))}...");
            
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception("Gemini API key is empty");
            }
        }

        public async Task<string> SolveMathProblem(string question)
        {
            try
            {
                _logger.LogInformation($"Processing question: {question}");
                
                var answer = await TryAllAPIVariants(question);
                
                if (!string.IsNullOrWhiteSpace(answer))
                {
                    return answer;
                }
                
                throw new Exception("Все варианты API не сработали. Проверь API ключ в Google AI Studio.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SolveMathProblem: {ex.Message}");
                throw new Exception($"Ошибка при получении ответа: {ex.Message}");
            }
        }
        
        private async Task<string> TryAllAPIVariants(string question)
        {
            var models = new[] 
            {
               "gemini-2.5-flash",       
               "gemini-2.5-pro",        
               "gemini-2.0-flash",        
               "gemini-exp-1206"
            };
            
            var urlTemplates = new[]
            {
                "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}",
                "https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={key}"
            };
            
            foreach (var urlTemplate in urlTemplates)
            {
                foreach (var model in models)
                {
                    try
                    {
                        _logger.LogInformation($"Пробуем: {model} с шаблоном {urlTemplate}");
                        var answer = await CallGeminiAPI(question, model, urlTemplate);
                        if (!string.IsNullOrWhiteSpace(answer))
                        {
                            _logger.LogInformation($"Успех с моделью: {model}");
                            return answer;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Модель {model} не сработала: {ex.Message}");
                    }
                }
            }
            
            return string.Empty;
        }
        
        private async Task<string> CallGeminiAPI(string question, string model, string urlTemplate)
        {
            var url = urlTemplate
                .Replace("{model}", model)
                .Replace("{key}", _apiKey);
            
            _logger.LogInformation($"Calling Gemini API: {url.Substring(0, Math.Min(80, url.Length))}...");

            // УЛУЧШЕННЫЙ ПРОМПТ - отвечает на всё!
            var systemPrompt = @"Ты — МАИ (Math AI Assistant), умный AI-помощник созданный для помощи студентам.

Твои возможности:
1. Решать математические задачи любой сложности (алгебра, геометрия, тригонометрия, математический анализ, линейная алгебра и т.д.) с подробными объяснениями и пошаговыми решениями.
2. Решать задачи по физике, химии, информатике и другим предметам такие как программирование, теория вероятностей, статистика и т.д.
3. Объяснять сложные концепции простым языком и конечно конкретной теорией тоже чтобы было и понятно и точно. Пояснить решения и давать интуицию, а не просто выдавать ответ
4. Отвечать на общие вопросы
5. Помогать с учёбой и домашними заданиями
6. Вести дружескую беседу

ВАЖНО:
- Если это математическая/физическая задача — покажи ПОШАГОВОЕ решение с объяснениями с максимальной аналитической точностью. Не пропускай шаги и не делай предположений. Покажи все формулы, вычисления и объяснения.
- Если это обычный вопрос — дай полезный и понятный ответ
- Пиши на русском языке или на языке вопроса
- Будь дружелюбным и полезным
- Форматируй ответ для удобного чтения (используй абзацы, списки где нужно) и по возможности добавляй примеры из реальной жизни и реальных источников чтобы было достоверно";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"{systemPrompt}\n\nВопрос пользователя: {question}"
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,  // Баланс между точностью и креативностью
                    maxOutputTokens = 2000,  // Больше токенов для развёрнутых ответов
                    topP = 0.95,
                    topK = 40
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API Error: Status: {response.StatusCode}");
                _logger.LogError($"Response: {responseText}");
                
                // Парсим ошибку
                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(responseText);
                    if (errorJson.TryGetProperty("error", out var errorObj))
                    {
                        if (errorObj.TryGetProperty("message", out var message))
                        {
                            var errorMessage = message.GetString();
                            throw new Exception($"{model}: {errorMessage}");
                        }
                    }
                }
                catch
                {
                    // Если не удалось распарсить ошибку, выбрасываем общее исключение
                }
                
                throw new Exception($"Gemini API вернул ошибку: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseText);

            if (!result.TryGetProperty("candidates", out var candidates) || 
                candidates.GetArrayLength() == 0)
            {
                throw new Exception("Неверный формат ответа от API");
            }

            var answer = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return answer ?? string.Empty;
        }
        
        public async Task<List<string>> GetAvailableModels()
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
                _logger.LogInformation($"Getting available models from: {url}");
                
                var response = await _httpClient.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Available models response: {responseText}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get models: {response.StatusCode}");
                    return new List<string>();
                }
                
                var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                var models = new List<string>();
                
                foreach (var model in result.GetProperty("models").EnumerateArray())
                {
                    var modelName = model.GetProperty("name").GetString();
                    if (modelName != null)
                    {
                        var shortName = modelName.Replace("models/", "");
                        models.Add(shortName);
                        _logger.LogInformation($"Found model: {shortName}");
                    }
                }
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting models: {ex.Message}");
                return new List<string>();
            }
        }
    }
}