using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/MessagesController")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class MessagesController : Controller
    {
        private readonly IAiService _aiService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IAiService aiService, ILogger<MessagesController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Получить все сообщения из базы данных
        /// </summary>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetAllMessage")]
        [ProducesResponseType(typeof(List<Message>), 200)]
        [ProducesResponseType(500)]
        public IActionResult GetAllMessage()
        {
            try
            {
                using var context = new GameShopContext();
                List<Message> messages = context.Messages.ToList();
                return Json(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка сообщений");
                return StatusCode(500, "Ошибка при получении списка сообщений");
            }
        }

        /// <summary>
        /// Отправить сообщение ИИ-ассистенту
        /// </summary>
        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("SendMessage")]
        [ProducesResponseType(typeof(Message), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendMessage([FromBody] Message request)
        {
            try
            {
                _logger.LogInformation($"Получено сообщение: {request.MessageText}");

                using var context = new GameShopContext();

                var userMessage = new Message
                {
                    UserId = request.UserId,
                    MessageText = request.MessageText,
                    IsFromGuest = request.IsFromGuest,
                    MessageDate = DateTime.UtcNow
                };

                context.Messages.Add(userMessage);
                await context.SaveChangesAsync();

                _logger.LogInformation("Сообщение пользователя сохранено в БД");

                var aiResponseText = await _aiService.GetResponseAsync(request.MessageText);

                _logger.LogInformation($"Получен ответ от ИИ: {aiResponseText}");

                var botMessage = new Message
                {
                    UserId = 29,
                    MessageText = aiResponseText,
                    IsFromGuest = false,
                    MessageDate = DateTime.UtcNow
                };

                context.Messages.Add(botMessage);
                await context.SaveChangesAsync();

                _logger.LogInformation("Ответ ИИ сохранен в БД");

                return Ok(new
                {
                    messageText = botMessage.MessageText,
                    messageDate = botMessage.MessageDate,
                    isFromBot = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения");
                return StatusCode(500, $"Ошибка при обработке сообщения: {ex.Message}");
            }
        }
    }
}