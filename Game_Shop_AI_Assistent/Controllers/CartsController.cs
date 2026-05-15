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

        /// <summary>
        /// Добавить игру в корзину пользователя
        /// </summary>
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || request.UserId <= 0 || request.GameId <= 0)
                    return BadRequest(new { success = false, message = "Неверные параметры запроса" });

                // Проверяем существование пользователя
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                // Проверяем существование игры
                var game = await _context.Games.FindAsync(request.GameId);
                if (game == null)
                    return NotFound(new { success = false, message = "Игра не найдена" });

                // Проверяем, не купил ли пользователь уже эту игру
                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.GameId == request.GameId);

                if (existingPurchase != null)
                    return BadRequest(new { success = false, message = "Вы уже приобрели эту игру" });

                // Ищем существующий товар в корзине
                var existingItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.GameId == request.GameId);

                if (existingItem != null)
                {
                    // Увеличиваем количество
                    existingItem.Quantity += request.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Количество игры '{game.Title}' увеличено до {existingItem.Quantity}",
                        cartItem = new
                        {
                            existingItem.Id,
                            existingItem.UserId,
                            existingItem.GameId,
                            existingItem.Quantity,
                            gameTitle = game.Title,
                            gamePrice = game.Price,
                            totalPrice = game.Price * existingItem.Quantity
                        }
                    });
                }

                // Создаем новый элемент корзины
                var cartItem = new Cart
                {
                    UserId = request.UserId,
                    GameId = request.GameId,
                    Quantity = request.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Игра '{game.Title}' добавлена в корзину",
                    cartItem = new
                    {
                        cartItem.Id,
                        cartItem.UserId,
                        cartItem.GameId,
                        cartItem.Quantity,
                        gameTitle = game.Title,
                        gamePrice = game.Price,
                        totalPrice = game.Price * cartItem.Quantity
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Получить корзину пользователя
        /// </summary>
        [HttpGet("GetUserCart/{userId}")]
        public async Task<IActionResult> GetUserCart(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var cartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Game)
                    .Select(c => new
                    {
                        c.Id,
                        c.UserId,
                        c.GameId,
                        c.Quantity,
                        c.CreatedAt,
                        c.UpdatedAt,
                        game = new
                        {
                            c.Game.Id,
                            c.Game.Title,
                            c.Game.Price,
                            c.Game.Description,
                            c.Game.Developer,
                            c.Game.Platform,
                            c.Game.ImageUrl,
                            c.Game.ReleaseDate
                        },
                        totalPrice = c.Game.Price * c.Quantity
                    })
                    .ToListAsync();

                var totalAmount = cartItems.Sum(i => i.totalPrice);
                var itemsCount = cartItems.Sum(i => i.Quantity);

                return Ok(new
                {
                    success = true,
                    userId = userId,
                    userName = user.Login ?? user.Email,
                    items = cartItems,
                    totalItems = itemsCount,
                    totalAmount = totalAmount,
                    currency = "RUB"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Обновить количество товара в корзине
        /// </summary>
        [HttpPut("UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                if (request == null || request.CartId <= 0 || request.Quantity <= 0)
                    return BadRequest(new { success = false, message = "Неверные параметры запроса" });

                var item = await _context.Carts
                    .Include(c => c.Game)
                    .FirstOrDefaultAsync(c => c.Id == request.CartId);

                if (item == null)
                    return NotFound(new { success = false, message = "Товар в корзине не найден" });

                item.Quantity = request.Quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Количество обновлено",
                    cartItem = new
                    {
                        item.Id,
                        item.UserId,
                        item.GameId,
                        item.Quantity,
                        gameTitle = item.Game?.Title,
                        gamePrice = item.Game?.Price ?? 0,
                        totalPrice = (item.Game?.Price ?? 0) * request.Quantity
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Удалить товар из корзины
        /// </summary>
        [HttpDelete("RemoveFromCart/{cartId}")]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            try
            {
                var item = await _context.Carts
                    .Include(c => c.Game)
                    .FirstOrDefaultAsync(c => c.Id == cartId);

                if (item == null)
                    return NotFound(new { success = false, message = "Товар в корзине не найден" });

                var gameTitle = item.Game?.Title ?? "Игра";
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"'{gameTitle}' удалена из корзины"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Очистить всю корзину пользователя
        /// </summary>
        [HttpDelete("ClearCart/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var items = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                var itemsCount = items.Count;
                _context.Carts.RemoveRange(items);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Корзина очищена. Удалено {itemsCount} товаров"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Получить количество товаров в корзине
        /// </summary>
        [HttpGet("GetCartItemsCount/{userId}")]
        public async Task<IActionResult> GetCartItemsCount(int userId)
        {
            try
            {
                var count = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Ok(new
                {
                    success = true,
                    userId = userId,
                    itemsCount = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Проверить, есть ли игра в корзине у пользователя
        /// </summary>
        [HttpGet("CheckInCart/{userId}/{gameId}")]
        public async Task<IActionResult> CheckInCart(int userId, int gameId)
        {
            try
            {
                var item = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId);

                return Ok(new
                {
                    success = true,
                    inCart = item != null,
                    quantity = item?.Quantity ?? 0,
                    cartId = item?.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
    }

    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int GameId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int CartId { get; set; }
        public int Quantity { get; set; }
    }
}