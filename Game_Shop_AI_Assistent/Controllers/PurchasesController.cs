using System.Text.Json.Serialization;
using GameShop.Context;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/PurchasesController")]
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    public class PurchasesController : Controller
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

        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetUserPurchases/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetUserPurchases(int userId)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Game)
                    .Select(p => new
                    {
                        p.Id,
                        p.UserId,
                        p.GameId,
                        GameName = p.Game.Title,
                        p.PurchaseDate,
                        p.ActivationKey,
                        p.KeyStatus
                    })
                    .ToListAsync();

                if (purchases.Count == 0)
                    return NotFound("Покупки не найдены");

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка покупок для userId={UserId}", userId);
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("BuyGame")]
        public async Task<ActionResult> BuyGame(
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
                    return BadRequest("Пользователь не найден");
                }
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("У пользователя {UserId} не указана почта", userId);
                    return BadRequest("У пользователя не указана почта");
                }

                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                {
                    _logger.LogWarning("Игра {GameId} не найдена", gameId);
                    return BadRequest("Игра не найдена");
                }

                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);
                if (existingPurchase != null)
                {
                    _logger.LogWarning("Игра {GameId} уже куплена пользователем {UserId}", gameId, userId);
                    return Conflict("Игра уже куплена этим пользователем");
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
                _logger.LogInformation("Покупка {PurchaseId} сохранена в БД", purchase.Id);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Отправка письма с ключом на {Email}", user.Email);
                        await _emailService.SendActivationKeyAsync(user.Email, game.Title, activationKey);
                        _logger.LogInformation("Письмо успешно отправлено");
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
                    purchase.ActivationKey,
                    purchase.KeyStatus,
                    Message = "Покупка успешна! Ключ отправлен на вашу почту."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в BuyGame: userId={UserId}, gameId={GameId}", userId, gameId);

                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new
                    {
                        error = "Произошла ошибка при покупке игры",
                        details = ex.Message,
                        inner = ex.InnerException?.Message
                    });
                }

                return StatusCode(500, "Произошла ошибка при покупке игры");
            }
        }

        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("AddPurchases")]
        public async Task<ActionResult> AddPurchases(
            [FromForm] int userId,
            [FromForm] int gameId,
            [FromForm] DateTime purchaseDate,
            [FromForm] string activationKey = null)
        {
            try
            {
                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                    return Conflict("Игра уже куплена этим пользователем");

                if (string.IsNullOrEmpty(activationKey))
                {
                    activationKey = GenerateActivationKey();
                }

                var purchase = new Purchase
                {
                    UserId = userId,
                    GameId = gameId,
                    PurchaseDate = purchaseDate,
                    ActivationKey = activationKey,
                    KeyStatus = "active"
                };

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                var game = await _context.Games.FindAsync(gameId);

                if (!string.IsNullOrWhiteSpace(user?.Email) && game != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendActivationKeyAsync(user.Email, game.Title, activationKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка отправки письма при добавлении покупки");
                        }
                    });
                }

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при внесении данных о покупке");
                return StatusCode(500, "Произошла ошибка при внесении данных о покупке");
            }
        }

        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetPurchase/{purchaseId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetPurchase(int purchaseId)
        {
            try
            {
                var purchase = await _context.Purchases
                    .Where(p => p.Id == purchaseId)
                    .Select(p => new
                    {
                        Purchase = p,
                        GameTitle = p.Game.Title
                    })
                    .FirstOrDefaultAsync();

                if (purchase == null)
                    return NotFound("Покупка не найдена");

                return Ok(new
                {
                    Id = purchase.Purchase.Id,
                    UserId = purchase.Purchase.UserId,
                    GameId = purchase.Purchase.GameId,
                    PurchaseDate = purchase.Purchase.PurchaseDate,
                    ActivationKey = purchase.Purchase.ActivationKey,
                    KeyStatus = purchase.Purchase.KeyStatus,
                    GameTitle = purchase.GameTitle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о покупке {PurchaseId}", purchaseId);
                return StatusCode(500, "Ошибка при получении информации о покупке");
            }
        }

        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetRecentPurchases/{userId}")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetRecentPurchases(int userId, [FromQuery] int count = 5)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(count)
                    .ToListAsync();

                if (purchases.Count == 0)
                    return NotFound("Покупки не найдены");

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка покупок для userId={UserId}", userId);
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("GenerateNewActivationKey/{purchaseId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GenerateNewActivationKey(int purchaseId)
        {
            try
            {
                var purchase = await _context.Purchases.FindAsync(purchaseId);

                if (purchase == null)
                    return NotFound("Покупка не найдена");

                string newActivationKey = GenerateActivationKey();
                purchase.ActivationKey = newActivationKey;
                purchase.KeyStatus = "active";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    NewActivationKey = newActivationKey,
                    Message = "Новый ключ активации успешно сгенерирован"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации нового ключа для purchaseId={PurchaseId}", purchaseId);
                return StatusCode(500, "Ошибка при генерации нового ключа");
            }
        }

        [ApiExplorerSettings(GroupName = "v2")]
        [HttpDelete("DeleteAllUserPurchases/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteAllUserPurchases(int userId)
        {
            try
            {
                var userPurchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (userPurchases.Count == 0)
                    return NotFound("Покупки не найдены");

                _context.Purchases.RemoveRange(userPurchases);
                await _context.SaveChangesAsync();

                return Ok($"Все покупки пользователя (всего {userPurchases.Count}) успешно удалены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении покупок пользователя {UserId}", userId);
                return StatusCode(500, "Произошла ошибка при удалении покупок пользователя");
            }
        }

        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetPurchasesByKeyStatus/{keyStatus}")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetPurchasesByKeyStatus(string keyStatus)
        {
            try
            {
                if (!new[] { "active", "used", "revoked" }.Contains(keyStatus))
                    return BadRequest("Неверный статус ключа. Допустимые значения: active, used, revoked");

                var purchases = await _context.Purchases
                    .Where(p => p.KeyStatus == keyStatus)
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка покупок по статусу {KeyStatus}", keyStatus);
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        private string GenerateActivationKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}