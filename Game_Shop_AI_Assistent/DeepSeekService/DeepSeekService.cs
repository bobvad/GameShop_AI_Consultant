using System.Text;
using System.Text.Json;

public interface IAiService
{
    Task<string> GetResponseAsync(string userMessage);
}

public class DeepSeekService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeepSeekService> _logger;

    public DeepSeekService(HttpClient httpClient, IConfiguration configuration, ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        try
        {
            _logger.LogInformation($"Отправка запроса к DeepSeek: {userMessage}");

            var apiKey = _configuration["DeepSeek:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("API ключ DeepSeek не настроен");
                return "API ключ не настроен. Проверьте appsettings.json";
            }

            var requestData = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = "Ты помощник для рекомендации игр в магазине. Отвечай на русском. Рекомендуй только игры, которые есть в нашем каталоге." },
                    new { role = "user", content = userMessage }
                },
                max_tokens = 500,
                temperature = 0.7,
                stream = false
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("Accept", "application/json");

            var jsonContent = JsonSerializer.Serialize(requestData);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation($"Отправка запроса к DeepSeek API...");

            var response = await _httpClient.SendAsync(request);

            _logger.LogInformation($"Статус ответа DeepSeek: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Ошибка API DeepSeek: {response.StatusCode}, Content: {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Ошибка аутентификации: неверный API ключ DeepSeek. Проверьте ключ в appsettings.json";
                }

                return $"Ошибка API DeepSeek: {response.StatusCode}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Ответ DeepSeek получен");

            using var jsonDoc = JsonDocument.Parse(responseContent);

            var content = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "Не удалось получить ответ от ИИ";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в DeepSeekService");
            return $"Извините, произошла ошибка при обращении к ИИ: {ex.Message}";
        }
    }
}