using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Game_Shop_AI_Assistent.Modell;
using GameShop.Context;
using Game_Shop_AI_Assistent.Services;

namespace Game_Shop_AI_Assistent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly GameShopContext _context;
        private readonly IEmailService _emailService;

        public PurchaseController(GameShopContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("GetAvailableKeysCount/{gameId}")]
        public async Task<ActionResult<object>> GetAvailableKeysCount(int gameId)
        {
            var availableCount = await _context.GameKeys
                .CountAsync(k => k.GameId == gameId && !k.IsUsed);

            var game = await _context.Games.FindAsync(gameId);

            if (game == null)
                return NotFound(new { success = false, message = "Игра не найдена" });

            return Ok(new
            {
                gameId = gameId,
                gameTitle = game.Title,
                availableKeysCount = availableCount,
                maxAvailable = availableCount
            });
        }

        [HttpGet("GetBatchAvailableKeys")]
        public async Task<ActionResult<object>> GetBatchAvailableKeys([FromQuery] string ids)
        {
            var gameIds = ids.Split(',').Select(int.Parse).ToList();
            var result = new List<object>();

            foreach (var gameId in gameIds)
            {
                var availableCount = await _context.GameKeys
                    .CountAsync(k => k.GameId == gameId && !k.IsUsed);

                var game = await _context.Games.FindAsync(gameId);

                result.Add(new
                {
                    gameId = gameId,
                    gameTitle = game?.Title ?? "Unknown",
                    availableKeysCount = availableCount,
                    maxAvailable = availableCount
                });
            }

            return Ok(result);
        }

        [HttpPost("BuyGame")]
        public async Task<ActionResult<object>> BuyGame([FromForm] PurchaseRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var game = await _context.Games.FindAsync(request.GameId);
                if (game == null)
                    return NotFound(new { success = false, message = "Игра не найдена" });

                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var availableKey = await _context.GameKeys
                    .Where(k => k.GameId == request.GameId && !k.IsUsed)
                    .FirstOrDefaultAsync();

                if (availableKey == null)
                    return BadRequest(new { success = false, message = "Нет доступных ключей для этой игры" });

                var purchase = new Purchase
                {
                    UserId = request.UserId,
                    GameId = request.GameId,
                    Price = game.Price,
                    PurchaseDate = DateTime.UtcNow,
                    ActivationKey = availableKey.Key,
                    KeyStatus = "active"
                };

                await _context.Purchases.AddAsync(purchase);
                _context.GameKeys.Remove(availableKey);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _emailService.SendActivationKeyAsync(
                    user.Email,
                    user.Login,
                    game.Title,
                    availableKey.Key,
                    game.Platform ?? "PC"
                );

                return Ok(new
                {
                    success = true,
                    message = "Покупка успешно совершена",
                    gameId = game.Id,
                    gameTitle = game.Title,
                    key = availableKey.Key,
                    purchaseDate = purchase.PurchaseDate
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost("PurchaseMultipleGames")]
        public async Task<ActionResult<object>> PurchaseMultipleGames([FromForm] BatchPurchaseRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var successfulPurchases = new List<object>();
            var errors = new List<string>();
            var allKeys = new List<string>();

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { success = false, message = "Пользователь не найден" });

            foreach (var item in request.Items)
            {
                try
                {
                    var game = await _context.Games.FindAsync(item.GameId);
                    if (game == null)
                    {
                        errors.Add($"Игра с ID {item.GameId} не найдена");
                        continue;
                    }

                    var keyToRemove = await _context.GameKeys
                        .Where(k => k.GameId == item.GameId && !k.IsUsed)
                        .FirstOrDefaultAsync();

                    if (keyToRemove == null)
                    {
                        errors.Add($"Нет ключей для игры {game.Title}");
                        continue;
                    }

                    var purchase = new Purchase
                    {
                        UserId = request.UserId,
                        GameId = item.GameId,
                        Price = game.Price,
                        PurchaseDate = DateTime.UtcNow,
                        ActivationKey = keyToRemove.Key,
                        KeyStatus = "active"
                    };

                    await _context.Purchases.AddAsync(purchase);
                    _context.GameKeys.Remove(keyToRemove);

                    allKeys.Add($"<b>{game.Title}</b>: {keyToRemove.Key}");

                    successfulPurchases.Add(new
                    {
                        success = true,
                        gameId = game.Id,
                        gameTitle = game.Title,
                        key = keyToRemove.Key
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка при покупке игры ID {item.GameId}: {ex.Message}");
                }
            }

            if (successfulPurchases.Any())
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (allKeys.Any())
                {
                    await _emailService.SendMultipleKeysEmail(
                        user.Email,
                        user.Login,
                        allKeys
                    );
                }
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return Ok(new
            {
                successCount = successfulPurchases.Count,
                errors = errors,
                purchases = successfulPurchases
            });
        }

        [HttpGet("GetUserPurchases/{userId}")]
        public async Task<ActionResult<List<UserPurchaseInfo>>> GetUserPurchases(int userId)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (purchases == null || !purchases.Any())
                    return Ok(new List<UserPurchaseInfo>());

                var result = purchases.Select(p => new UserPurchaseInfo
                {
                    Id = p.Id,
                    GameId = p.GameId,
                    Price = p.Price,
                    PurchaseDate = p.PurchaseDate,
                    ActivationKey = p.ActivationKey
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
            }
        }

        public class UserPurchaseInfo
        {
            public int Id { get; set; }
            public int GameId { get; set; }
            public decimal Price { get; set; }
            public DateTime PurchaseDate { get; set; }
            public string ActivationKey { get; set; } = string.Empty;
        }

    }

    public class PurchaseRequest
    {
        public int UserId { get; set; }
        public int GameId { get; set; }
    }

    public class BatchPurchaseRequest
    {
        public int UserId { get; set; }
        public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }

    public class PurchaseItem
    {
        public int GameId { get; set; }
    }
}