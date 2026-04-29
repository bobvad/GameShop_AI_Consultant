using GameShop.Context;
using Game_Shop_AI_Assistent.GigaChat_LLM.API_UP_02.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly GigaChatService _gigaChat;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(GigaChatService gigaChat, ILogger<MessagesController> logger)
        {
            _gigaChat = gigaChat;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromForm] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.MessageText))
                return BadRequest(new { error = "Сообщение обязательно" });

            try
            {
                var sessionId = request.SessionId ?? $"user_{request.UserId ?? 0}";
                var reply = await _gigaChat.SendMessageAsync(request.MessageText, sessionId);

                return Ok(new
                {
                    success = true,
                    message = reply,
                    isFromBot = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения");
                return StatusCode(500, new { error = "Ошибка сервера" });
            }
        }
    }

    public class ChatRequest
    {
        public string MessageText { get; set; }
        public int? UserId { get; set; }
        public string SessionId { get; set; }
        public bool IsFromGuest { get; set; }
    }
}