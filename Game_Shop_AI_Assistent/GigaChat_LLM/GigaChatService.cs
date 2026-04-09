using Game_Shop_AI_Assistent.GigaChat_LLM.Models;
using GameShop.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Game_Shop_AI_Assistent.GigaChat_LLM
{
    /// <summary>
    /// Сервис для получения игровых рекомендаций через GigaChat API
    /// </summary>
    public class GigaChatService
    {
        private static string ClientId = "0199d470-bb93-7ce2-b0df-620ead27395d";
        private static string AuthorizationKey = "MDE5OWQ0NzAtYmI5My03Y2UyLWIwZGYtNjIwZWFkMjczOTVkOjQwNjdkNDdhLWY1MTYtNGZiYS05ZGM5LTg0MDAwNDExNTUwNQ==";
        private static string? Token = null;
        private static DateTime TokenExpirationTime;

        private static readonly Dictionary<int, DateTime> _lastRecommendations = new();

        private readonly GameShopContext _context;
        private readonly ILogger<GigaChatService> _logger;

        /// <summary>
        /// Системный промпт — роль и правила для ИИ
        /// </summary>
        private const string SystemPrompt = @"Ты — экспертный игровой рекомендательный сервис.

Твоя задача — рекомендовать видеоигры пользователям на основе их запросов и предпочтений.

ВАЖНО: 
- Все рекомендации должны быть РЕАЛЬНЫМИ существующими играми
- Учитывай жанровые предпочтения пользователя

Для каждой рекомендации указывай:
1. Название игры
2. Жанр и платформа
3. Краткое описание (2-3 предложения)
4. Почему эта игра подходит под запрос

Формат ответа:
По вашему запросу я рекомендую:

[Название игры] | [Жанр] | [Платформа]
[Описание игры и геймплея]
Почему подходит: [обоснование]

Давай 2-4 рекомендации на запрос. Будь дружелюбным!";

        public GigaChatService(GameShopContext context, ILogger<GigaChatService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 🔹 Основные методы API

        /// <summary>
        /// Получение игровых рекомендаций по текстовому запросу
        /// </summary>
        public async Task<string> GetGameRecommendation(string userRequest, List<Request.Message>? conversationHistory = null)
        {
            try
            {
                _logger.LogInformation($"[GigaChat] Запрос: {userRequest}");

                await EnsureTokenAsync();

                conversationHistory ??= new List<Request.Message>();

                if (conversationHistory.Count == 0 || !conversationHistory.Any(m => m.role == "system"))
                {
                    conversationHistory.Insert(0, new Request.Message
                    {
                        role = "system",
                        content = SystemPrompt
                    });
                }

                conversationHistory.Add(new Request.Message
                {
                    role = "user",
                    content = userRequest
                });

                var response = await GetAnswer(Token!, conversationHistory);

                if (response?.choices?.Count > 0)
                {
                    string assistantResponse = response.choices[0].message.content;

                    conversationHistory.Add(new Request.Message
                    {
                        role = "assistant",
                        content = assistantResponse
                    });

                    return assistantResponse;
                }

                return "🎮 Не удалось получить рекомендации. Попробуйте переформулировать запрос.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GigaChat] Ошибка при получении рекомендации");
                return $"⚠️ Ошибка: {ex.Message}. Попробуйте позже.";
            }
        }

        /// <summary>
        /// Персональные рекомендации на основе покупок пользователя
        /// </summary>
        public async Task<string> GetPersonalizedRecommendation(int userId)
        {
            try
            {
                _logger.LogInformation($"[GigaChat] Персональная рекомендация для {userId}");

                await EnsureTokenAsync();

                var prompt = await BuildPersonalizedPrompt(userId);

                var messages = new List<Request.Message>
                {
                    new() { role = "system", content = SystemPrompt },
                    new() { role = "user", content = prompt }
                };

                var response = await GetAnswer(Token!, messages);

                return response?.choices?.FirstOrDefault()?.message?.content
                       ?? "🎮 Не удалось подобрать рекомендации. Попробуйте позже.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GigaChat] Ошибка для пользователя {userId}");
                return "⚠️ Что-то пошло не так. Попробуйте позже.";
            }
        }

        /// <summary>
        /// Автоматическая рекомендация при входе в приложение
        /// </summary>
        public async Task<AutoRecommendation?> GetAutoRecommendation(int userId)
        {
            try
            {
                _logger.LogInformation($"[GigaChat] Авторекомендация для {userId}");

                await EnsureTokenAsync();

                // ✅ ИСПРАВЛЕНО: Получаем данные о пользователе
                var hasPurchases = await _context.Purchases.AnyAsync(p => p.UserId == userId);
                var hasInCart = await _context.Carts.AnyAsync(c => c.UserId == userId);

                // Новый пользователь — даём универсальные рекомендации
                if (!hasPurchases && !hasInCart)
                {
                    var newUserPrompt = @"Пользователь только начал пользоваться приложением.

Порекомендуй 4 популярные игры разных жанров для новичка:
1. Бесплатная игра с низким порогом входа
2. Сюжетная игра с захватывающим повествованием  
3. Казуальная игра для расслабления
4. Хит последнего года

Для каждой: название, платформа, жанр, краткое описание.";

                    var messages = new List<Request.Message>
                    {
                        new() { role = "system", content = SystemPrompt },
                        new() { role = "user", content = newUserPrompt }
                    };

                    var response = await GetAnswer(Token!, messages);

                    if (response?.choices?.Count > 0)
                    {
                        return new AutoRecommendation
                        {
                            Title = "🎮 Добро пожаловать! Начни с этих игр",
                            Description = response.choices[0].message.content,
                            Type = "welcome",
                            ShowOnLogin = true,
                            Priority = 10
                        };
                    }
                }

                // Не показывать рекомендации чаще 1 раза в 24 часа
                if (_lastRecommendations.ContainsKey(userId) &&
                    _lastRecommendations[userId] > DateTime.UtcNow.AddHours(-24))
                {
                    return null;
                }

                // ✅ ИСПРАВЛЕНО: Собираем предпочтения и формируем промпт
                var userPreferences = await GetUserPreferences(userId);
                var autoPrompt = BuildAutoRecommendationPrompt(userPreferences);

                var autoMessages = new List<Request.Message>
                {
                    new() { role = "system", content = SystemPrompt },
                    new() { role = "user", content = autoPrompt }
                };

                var autoResponse = await GetAnswer(Token!, autoMessages);

                if (autoResponse?.choices?.Count > 0)
                {
                    _lastRecommendations[userId] = DateTime.UtcNow;

                    return new AutoRecommendation
                    {
                        Title = GetRecommendationTitle(userPreferences),
                        Description = autoResponse.choices[0].message.content,
                        Type = "personalized",
                        ShowOnLogin = true,
                        Priority = 5
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GigaChat] Ошибка авторекомендации для {userId}");
                return null;
            }
        }

        #endregion

        #region 🔐 Работа с токеном

        public async Task<string?> GetToken()
        {
            string rqUID = Guid.NewGuid().ToString();
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true // ⚠️ Только для dev!
            };

            using var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("RqUID", rqUID);
            request.Headers.Add("Authorization", $"Basic {AuthorizationKey}");

            var data = new List<KeyValuePair<string, string>>
            {
                new("scope", "GIGACHAT_API_PERS")
            };
            request.Content = new FormUrlEncodedContent(data);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<ResponseToken>(content);
                _logger.LogInformation("[GigaChat] Токен получен");
                return token?.access_token;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[GigaChat] Ошибка токена: {response.StatusCode} - {error}");
                return null;
            }
        }

        public async Task<ResponseMessage?> GetAnswer(string token, List<Request.Message> messages)
        {
            string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("X-Client-ID", ClientId);

            var dataRequest = new Request
            {
                model = "GigaChat",
                stream = false,
                repetition_penalty = 1,
                messages = messages
            };

            var json = JsonConvert.SerializeObject(dataRequest);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseMessage>(content);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[GigaChat] Ошибка API: {response.StatusCode} - {error}");
                return null;
            }
        }

        private async Task EnsureTokenAsync()
        {
            if (Token == null || TokenExpirationTime <= DateTime.UtcNow)
            {
                Token = await GetToken();
                TokenExpirationTime = DateTime.UtcNow.AddMinutes(30);
                _logger.LogInformation("[GigaChat] Токен обновлён");
            }
        }

        #endregion

        #region 

        /// <summary>
        /// Формирование промпта на основе покупок пользователя
        /// </summary>
        private async Task<string> BuildPersonalizedPrompt(int userId)
        {
            var purchasedGames = await _context.Purchases
                .Include(p => p.Game)
                .Where(p => p.UserId == userId && p.Game != null)
                .Select(p => p.Game!)
                .ToListAsync();

            var cartGames = await _context.Carts
                .Include(c => c.Game)
                .Where(c => c.UserId == userId && c.Game != null)
                .Select(c => c.Game!)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Порекомендуй мне игры на основе моей истории:");
            sb.AppendLine();

            if (purchasedGames.Any())
            {
                sb.AppendLine("Купленные игры:");
                foreach (var game in purchasedGames.Take(5))
                {
                    sb.AppendLine($"  {game.Title}" );
                }
                sb.AppendLine();
            }

            if (cartGames.Any())
            {
                sb.AppendLine("В корзине:");
                foreach (var game in cartGames.Take(3))
                {
                    sb.AppendLine($"{game.Title}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("\nПосоветуй 3-4 игры, которые мне понравятся.");
            sb.AppendLine("Исключи игры, которые я уже купил.");
            sb.AppendLine("Укажи: название, жанр, платформу и почему мне это зайдёт.");

            return sb.ToString();
        }

        /// <summary>
        /// Сбор предпочтений пользователя
        /// </summary>
        private async Task<UserPreferences> GetUserPreferences(int userId)
        {
            var preferences = new UserPreferences();

            var purchasedGames = await _context.Purchases
                .Include(p => p.Game)
                .Where(p => p.UserId == userId && p.Game != null)
                .Select(p => p.Game!)
                .ToListAsync();

            var cartGames = await _context.Carts
                .Include(c => c.Game)
                .Where(c => c.UserId == userId && c.Game != null)
                .Select(c => c.Game!)
                .ToListAsync();

            preferences.PurchasedGames = purchasedGames;
            preferences.CartGames = cartGames;



            return preferences;
        }

        /// <summary>
        /// Формирование промпта для автоподбора
        /// </summary>
        private string BuildAutoRecommendationPrompt(UserPreferences prefs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Порекомендуй 3-4 игры на основе моей истории:");
            sb.AppendLine();

            if (prefs.PurchasedGames.Any())
            {
                sb.AppendLine("Купил и понравилось:");
                foreach (var game in prefs.PurchasedGames.Take(4))
                {
                    sb.AppendLine($"{game.Title} ");
                }
                sb.AppendLine();
            }

            if (prefs.CartGames.Any())
            {
                sb.AppendLine("Смотрю в корзине:");
                foreach (var game in prefs.CartGames.Take(3))
                {
                    sb.AppendLine($"{game.Title}");
                }
                sb.AppendLine();
            }

            if (prefs.TopGenres.Any())
                sb.AppendLine($"Любимые жанры: {string.Join(", ", prefs.TopGenres)}");

            if (prefs.TopPlatforms.Any())
                sb.AppendLine($"Платформа: {string.Join(", ", prefs.TopPlatforms)}");

            sb.AppendLine();
            sb.AppendLine("Посоветуй 3-4 игры, которые мне зайдут. Исключи уже купленные.");
            sb.AppendLine("Формат:");
            sb.AppendLine("[Название] | [Жанр] | [Платформа]");
            sb.AppendLine("[Описание]");
            sb.AppendLine("Почему подойдёт: [обоснование]");

            return sb.ToString();
        }

        private string GetRecommendationTitle(UserPreferences prefs)
        {
            if (prefs.CartGames.Any())
                return "На основе ваших интересов";
            else if (prefs.PurchasedGames.Any())
                return "Продолжение ваших предпочтений";
            else if (prefs.TopGenres.Any())
                return $"Подборка: {prefs.TopGenres.First()}";
            else
                return "Популярные игры для вас";
        }

        #endregion
    }

    /// <summary>
    /// Модель авто-рекомендации для фронтенда
    /// </summary>
    public class AutoRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 
        public bool ShowOnLogin { get; set; }
        public int Priority { get; set; } = 1;
    }

    /// <summary>
    /// Предпочтения пользователя
    /// </summary>
    public class UserPreferences
    {
        public List<Game> PurchasedGames { get; set; } = new();
        public List<Game> CartGames { get; set; } = new();
        public List<string> TopGenres { get; set; } = new();
        public List<string> TopPlatforms { get; set; } = new();
    }
}