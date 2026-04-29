using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/[controller]")]

    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly GameShopContext _context;

        public CartsController(GameShopContext context)
        {
            _context = context;
        }


        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromForm] int userId, [FromForm] int gameId, [FromForm] int quantity = 1)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest("Пользователь не найден");

            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return BadRequest("Игра не найдена");

            var existing = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }

            var cartItem = new Cart
            {
                UserId = userId,
                GameId = gameId,
                Quantity = quantity
            };

            _context.Carts.Add(cartItem);
            await _context.SaveChangesAsync();

            return Ok(cartItem);
        }


        [HttpGet("GetUserCart/{userId}")]
        public async Task<IActionResult> GetUserCart(int userId)
        {
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Game)
                .ToListAsync();

            return Ok(cartItems); 
        }


        [HttpPut("UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromForm] int cartId, [FromForm] int quantity)
        {
            var item = await _context.Carts.FindAsync(cartId);

            if (item == null)
                return NotFound();

            if (quantity <= 0)
                return BadRequest("Количество должно быть > 0");

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(item);
        }


        [HttpDelete("RemoveFromCart/{cartId}")]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var item = await _context.Carts.FindAsync(cartId);

            if (item == null)
                return NotFound();

            _context.Carts.Remove(item);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpDelete("ClearCart/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var items = await _context.Carts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.Carts.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpGet("GetCartItemsCount/{userId}")]
        public async Task<IActionResult> GetCartItemsCount(int userId)
        {
            var count = await _context.Carts
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Ok(count);
        }
    }
}