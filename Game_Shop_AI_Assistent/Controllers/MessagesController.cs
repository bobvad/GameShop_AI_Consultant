using GameShop.Context;
using Game_Shop_AI_Assistent.GigaChat_LLM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    /// <summary>
    /// Контроллер для отправки сообщений ИИ-боту (GigaChat) через Form
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class MessagesController : ControllerBase
    {
        private readonly ILogger<MessagesController> _logger;
        private readonly GigaChatService _gigaChatService;
        private readonly GameShopContext _context;

        public MessagesController(
            ILogger<MessagesController> logger,
            GigaChatService gigaChatService,
            GameShopContext context)
        {
            _logger = logger;
            _gigaChatService = gigaChatService;
            _context = context;
        }

        /// <summary>
        /// Отправить сообщение боту (через Form)
        /// </summary>
        /// <remarks>
        /// Формат запроса (application/x-www-form-urlencoded):
        /// 
        ///     userId=1&amp;messageText=Посоветуй+игры+про+космос&amp;isFromGuest=false
        /// 
        /// Или multipart/form-data с теми же полями.
        /// </remarks>
        [HttpPost("Send")]
        [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
        [ProducesResponseType(typeof(BotResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Send(
            [FromForm] int userId,
            [FromForm] string messageText,
            [FromForm] bool isFromGuest = true)
        {
            // 🔍 Валидация входных данных
            if (string.IsNullOrWhiteSpace(messageText))
                return BadRequest(new { error = "Поле messageText обязательно" });

            if (userId <= 0 && !isFromGuest)
                return BadRequest(new { error = "Неверный userId" });

            try
            {
                _logger.LogInformation($"[Bot] Запрос от userId={userId}: \"{messageText}\"");

                // 💾 1. Сохраняем сообщение пользователя
                var userMsg = new Message
                {
                    UserId = userId,
                    MessageText = messageText.Trim(),
                    IsFromGuest = isFromGuest,
                    IsFromBot = false,
                    MessageDate = DateTime.UtcNow
                };
                _context.Messages.Add(userMsg);
                await _context.SaveChangesAsync();

                // 🤖 2. Получаем ответ от GigaChat
                string botAnswer = await _gigaChatService.GetGameRecommendation(messageText);

                // 💾 3. Сохраняем ответ бота
                var botMsg = new Message
                {
                    UserId = userId,
                    MessageText = botAnswer,
                    IsFromGuest = isFromGuest,
                    IsFromBot = true,
                    MessageDate = DateTime.UtcNow
                };
                _context.Messages.Add(botMsg);
                await _context.SaveChangesAsync();

                // 📤 4. Возвращаем ответ клиенту
                return Ok(new BotResponse
                {
                    Success = true,
                    Message = botMsg.MessageText,
                    Timestamp = botMsg.MessageDate,
                    MessageId = botMsg.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Bot] Ошибка обработки сообщения");
                return StatusCode(500, new { error = "Ошибка сервера", details = ex.Message });
            }
        }

        /// <summary>
        /// 📜 Получить последние сообщения пользователя
        /// </summary>
        [HttpGet("History")]
        [ProducesResponseType(typeof(List<Message>), 200)]
        public async Task<IActionResult> GetHistory([FromQuery] int userId, [FromQuery] int limit = 20)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.MessageDate)
                    .Take(limit)
                    .ToListAsync();

                return Ok(messages.OrderBy(m => m.MessageDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки истории");
                return StatusCode(500, new { error = "Не удалось загрузить историю" });
            }
        }

        /// <summary>
        /// Очистить историю сообщений
        /// </summary>
        [HttpDelete("Clear")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Clear([FromQuery] int userId)
        {
            try
            {
                var msgs = await _context.Messages
                    .Where(m => m.UserId == userId)
                    .ToListAsync();

                _context.Messages.RemoveRange(msgs);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, deleted = msgs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка очистки истории");
                return StatusCode(500, new { error = "Не удалось очистить" });
            }
        }
    }

    /// <summary>
    /// Ответ бота для клиента
    /// </summary>
    public class BotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int MessageId { get; set; }
    }
}