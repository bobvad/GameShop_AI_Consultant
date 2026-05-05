using GameShop.Context;
using Game_Shop_AI_Assistent.Modell;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

        public static class KeyGenerator
        {
            public static string GenerateKey()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var result = new StringBuilder();

                for (int block = 0; block < 4; block++)
                {
                    if (block > 0)
                        result.Append("-");

                    for (int i = 0; i < 4; i++)
                    {
                        var index = RandomNumberGenerator.GetInt32(chars.Length);
                        result.Append(chars[index]);
                    }
                }

                return result.ToString();
            }
        }

        [HttpPost("BuyGame")]
        public async Task<IActionResult> BuyGame([FromForm] int userId, [FromForm] int gameId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return BadRequest("User not found");

                if (string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest("Email missing");

                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                    return BadRequest("Game not found");

                var existing = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

                string key;

                if (existing != null)
                {
                    key = existing.ActivationKey;
                }
                else
                {
                    key = KeyGenerator.GenerateKey();

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
                }

                var emailSent = await _emailService.SendActivationKeyAsync(
                    user.Email,
                    game.Title,
                    key
                );

                return Ok(new
                {
                    success = true,
                    message = emailSent ? "Success, email sent" : "Success, email failed",
                    key
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purchase error");
                return StatusCode(500, "Server error");
            }
        }

        [HttpGet("TestEmail")]
        public async Task<IActionResult> TestEmail()
        {
            var result = await _emailService.SendActivationKeyAsync(
                "test@gmail.com",
                "Test Game",
                "ABCD-EFGH-IJKL-MNOP"
            );

            return Ok(result);
        }
    }
}