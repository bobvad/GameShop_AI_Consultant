using Game_Shop_AI_Assistent.GigaChat_LLM.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Game_Shop_AI_Assistent.GigaChat_LLM.API_UP_02.Services
{
    public class GigaChatService
    {
        private static string ClientId = "0199d470-bb93-7ce2-b0df-620ead27395d";
        private static string AuthorizationKey = "MDE5OWQ0NzAtYmI5My03Y2UyLWIwZGYtNjIwZWFkMjczOTVkOjQwNjdkNDdhLWY1MTYtNGZiYS05ZGM5LTg0MDAwNDExNTUwNQ==";

        private static string _token;
        private static DateTime _tokenExpirationTime;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        private readonly ILogger<GigaChatService> _logger;
        private readonly IMemoryCache _cache;

        private const string SystemPrompt = "Ты - помощник онлайн-магазина игр. Отвечай кратко, полезно и по делу.";
        private const int MaxHistoryMessages = 10;
        private const int HistoryTtlMinutes = 30;

        public GigaChatService(ILogger<GigaChatService> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> SendMessageAsync(string userMessage, string sessionId = "default")
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("Сообщение не может быть пустым", nameof(userMessage));

            await EnsureTokenAsync();

            var history = GetHistory(sessionId);
            history.Add(new Request.Message { role = "user", content = userMessage });

            var messages = BuildMessagesWithHistory(history);
            var response = await CallGigaChatAsync(_token, messages);

            if (response?.choices?.FirstOrDefault()?.message?.content is string botReply)
            {
                history.Add(new Request.Message { role = "assistant", content = botReply });
                SaveHistory(sessionId, history);
                return botReply;
            }

            return "Не удалось получить ответ от нейросети";
        }

        public void ClearHistory(string sessionId = "default")
        {
            var cacheKey = $"gigachat_history_{sessionId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation($"История очищена для сессии: {sessionId}");
        }

        private List<Request.Message> GetHistory(string sessionId)
        {
            var cacheKey = $"gigachat_history_{sessionId}";
            return _cache.Get(cacheKey) as List<Request.Message> ?? new List<Request.Message>();
        }

        private void SaveHistory(string sessionId, List<Request.Message> history)
        {
            var cacheKey = $"gigachat_history_{sessionId}";
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(HistoryTtlMinutes))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, history, options);
        }

        private List<Request.Message> BuildMessagesWithHistory(List<Request.Message> history)
        {
            var messages = new List<Request.Message>
            {
                new Request.Message { role = "system", content = SystemPrompt }
            };

            var recentHistory = history.Skip(Math.Max(0, history.Count - MaxHistoryMessages));
            messages.AddRange(recentHistory);

            return messages;
        }

        private async Task EnsureTokenAsync()
        {
            await _tokenLock.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(_token) || _tokenExpirationTime <= DateTime.UtcNow)
                {
                    _token = await FetchNewTokenAsync();
                    _tokenExpirationTime = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogInformation("Токен обновлен");
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private async Task<string> FetchNewTokenAsync()
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("RqUID", Guid.NewGuid().ToString());
            request.Headers.Add("Authorization", $"Basic {AuthorizationKey}");
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = JsonConvert.DeserializeObject<ResponseToken>(content);
                return tokenData?.access_token;
            }

            _logger.LogError($"Ошибка получения токена: {response.StatusCode} - {content}");
            return null;
        }

        private async Task<ResponseMessage> CallGigaChatAsync(string token, List<Request.Message> messages)
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://gigachat.devices.sberbank.ru/api/v1/chat/completions");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("X-Client-ID", ClientId);

            var payload = new
            {
                model = "GigaChat",
                stream = false,
                messages = messages,
                repetition_penalty = 1.0
            };

            request.Content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ResponseMessage>(content);
            }

            _logger.LogError($"Ошибка вызова GigaChat: {response.StatusCode} - {content}");
            return null;
        }
    }
}