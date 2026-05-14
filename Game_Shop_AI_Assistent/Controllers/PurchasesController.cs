using GameShop.Context;
using Game_Shop_AI_Assistent.Modell;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/Purchases")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly GameShopContext _context;
        private readonly IEmailService _emailService;

        public PurchasesController(GameShopContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("BuyGame")]
        public async Task<IActionResult> BuyGame([FromForm] int userId, [FromForm] int gameId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return BadRequest(new { success = false, message = "Пользователь не найден" });

                if (string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { success = false, message = "У пользователя не указан email" });

                var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
                if (game == null)
                    return BadRequest(new { success = false, message = "Игра не найдена" });

                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                {
                    return BadRequest(new { success = false, message = "Вы уже приобрели эту игру" });
                }

                var gameKey = await _context.GameKeys
                    .FirstOrDefaultAsync(k => k.GameId == gameId && !k.IsUsed);

                if (gameKey == null)
                {
                    return BadRequest(new { success = false, message = "Нет доступных ключей для этой игры" });
                }

                gameKey.IsUsed = true;
                gameKey.UsedByUserId = userId;
                gameKey.UsedAt = DateTime.UtcNow;

                var purchase = new Purchase
                {
                    UserId = userId,
                    GameId = gameId,
                    ActivationKey = gameKey.Key,
                    PurchaseDate = DateTime.UtcNow,
                    KeyStatus = "active"
                };

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                bool emailSent = await _emailService.SendActivationKeyAsync(
                    user.Email,
                    user.Login ?? user.Email,
                    game.Title,
                    gameKey.Key,
                    game.Platform ?? "Steam"
                );

                return Ok(new
                {
                    success = true,
                    message = emailSent ? "Покупка успешна! Ключ отправлен на почту." : "Покупка успешна! Но не удалось отправить email.",
                    key = gameKey.Key,
                    emailSent = emailSent,
                    gameTitle = game.Title,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost("TestEmail")]
        public async Task<IActionResult> TestEmail([FromForm] string testEmail, [FromForm] string testGameName = "Тестовая игра")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testEmail))
                    return BadRequest(new { success = false, message = "Укажите email для теста" });

                string testKey = "TEST-" + Guid.NewGuid().ToString().ToUpper().Substring(0, 24);

                bool result = await _emailService.SendActivationKeyAsync(
                    testEmail,
                    "Тестовый пользователь",
                    testGameName,
                    testKey,
                    "Steam"
                );

                return Ok(new
                {
                    success = result,
                    message = result ? $"Письмо отправлено на {testEmail}" : "Ошибка отправки",
                    email = testEmail,
                    key = testKey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetAvailableKeys/{gameId}")]
        public async Task<IActionResult> GetAvailableKeys(int gameId)
        {
            try
            {
                int available = await _context.GameKeys.CountAsync(k => k.GameId == gameId && !k.IsUsed);
                int total = await _context.GameKeys.CountAsync(k => k.GameId == gameId);

                return Ok(new
                {
                    gameId,
                    availableKeys = available,
                    totalKeys = total,
                    hasKeys = available > 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetUserPurchases/{userId}")]
        public async Task<IActionResult> GetUserPurchases(int userId)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Game)
                    .Select(p => new
                    {
                        p.Id,
                        p.GameId,
                        GameName = p.Game != null ? p.Game.Title : "Неизвестная игра",
                        p.PurchaseDate,
                        p.KeyStatus,
                        p.ActivationKey,
                        Platform = p.Game != null ? p.Game.Platform : "Unknown"
                    })
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}