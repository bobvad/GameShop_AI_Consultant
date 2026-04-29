using System.Text.Json.Serialization;
using GameShop.Context;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Game_Shop_AI_Assistent.Modell;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/Purchases")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly GameShopContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<PurchasesController> _logger;
        private readonly IWebHostEnvironment _env;

        public PurchasesController(
            GameShopContext context,
            IEmailService emailService,
            ILogger<PurchasesController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _env = env;
        }

        [HttpPost("BuyGame")]
        public async Task<ActionResult<object>> BuyGame(
            [FromForm] int userId,
            [FromForm] int gameId)
        {
            try
            {
                _logger.LogInformation("Покупка: userId={UserId}, gameId={GameId}", userId, gameId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден", userId);
                    return BadRequest(new { message = "Пользователь не найден" });
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return BadRequest(new { message = "У пользователя не указана почта" });
                }

                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                {
                    _logger.LogWarning("Игра {GameId} не найдена", gameId);
                    return BadRequest(new { message = "Игра не найдена" });
                }

                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                {
                    return Ok(new
                    {
                        message = "Игра уже куплена",
                        key = existingPurchase.ActivationKey,
                        purchaseId = existingPurchase.Id
                    });
                }

                string activationKey = GenerateActivationKey();

                var purchase = new Purchase
                {
                    UserId = userId,
                    GameId = gameId,
                    PurchaseDate = DateTime.UtcNow,
                    ActivationKey = activationKey,
                    KeyStatus = "active"
                };

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Покупка {PurchaseId} сохранена", purchase.Id);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendActivationKeyAsync(
                            user.Email,
                            game.Title,
                            activationKey);
                        _logger.LogInformation("Письмо отправлено на {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка отправки письма на {Email}", user.Email);
                    }
                });

                return Ok(new
                {
                    purchase.Id,
                    purchase.UserId,
                    purchase.GameId,
                    GameName = game.Title,
                    purchase.PurchaseDate,
                    purchase.KeyStatus,
                    Key = activationKey,
                    Message = "Покупка успешна! Ключ отправлен на почту."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при покупке: userId={UserId}, gameId={GameId}", userId, gameId);

                return StatusCode(500, new
                {
                    message = "Ошибка при покупке",
                    details = _env.IsDevelopment() ? ex.Message : null
                });
            }
        }

        [HttpGet("GetUserPurchases/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserPurchases(int userId)
        {
            var purchases = await _context.Purchases
                .Where(p => p.UserId == userId)
                .Include(p => p.Game)
                .Select(p => new
                {
                    p.Id,
                    p.GameId,
                    GameName = p.Game.Title,
                    p.PurchaseDate,
                    p.KeyStatus,
                    p.ActivationKey,
                    Platform = p.Game.Platform,
                    ImageUrl = p.Game.ImageUrl,
                    Price = p.Game.Price
                })
                .ToListAsync();

            if (purchases.Count == 0)
                return NotFound(new { message = "Покупки не найдены" });

            return Ok(purchases);
        }

        [HttpPost("GenerateNewActivationKey/{purchaseId}")]
        public async Task<ActionResult> GenerateNewActivationKey(int purchaseId)
        {
            var purchase = await _context.Purchases.FindAsync(purchaseId);

            if (purchase == null)
                return NotFound(new { message = "Покупка не найдена" });

            purchase.ActivationKey = GenerateActivationKey();
            purchase.KeyStatus = "active";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Новый ключ сгенерирован" });
        }

        private string GenerateActivationKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}