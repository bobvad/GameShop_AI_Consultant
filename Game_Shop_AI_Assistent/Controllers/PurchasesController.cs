using GameShop.Context;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public PurchasesController(
            GameShopContext context,
            IEmailService emailService,
            ILogger<PurchasesController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("BuyGame")]
        public async Task<IActionResult> BuyGame([FromForm] int userId, [FromForm] int gameId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return BadRequest(new { message = "Пользователь не найден" });

                if (string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { message = "Email не указан" });

                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                    return BadRequest(new { message = "Игра не найдена" });

                var existing = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

                if (existing != null)
                {
                    return Ok(new
                    {
                        message = "Игра уже куплена",
                        key = existing.ActivationKey
                    });
                }

                string key = GenerateKey();

                var purchase = new Purchase
                {
                    UserId = userId,
                    GameId = gameId,
                    ActivationKey = key,
                    PurchaseDate = DateTime.UtcNow,
                    KeyStatus = "active"
                };

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                await _emailService.SendActivationKeyAsync(
                    user.Email,
                    game.Title,
                    key
                );

                return Ok(new
                {
                    success = true,
                    message = "Покупка успешна! Ключ отправлен на email",
                    key = key
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка покупки");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        private string GenerateKey()
        {
            var rnd = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return string.Join("-",
                Enumerable.Range(0, 4)
                .Select(_ => new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[rnd.Next(s.Length)]).ToArray())));
        }
    }
}