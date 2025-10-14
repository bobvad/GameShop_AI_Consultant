using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    public class CartsController : Controller
    {
        [HttpPost("AddToCart")]
        [ProducesResponseType(typeof(Cart), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult AddToCart([FromForm] int userId, [FromForm] int gameId, [FromForm] int quantity = 1)
        {
            try
            {
                using var context = new GameShopContext();

                Console.WriteLine($"Поиск пользователя {userId}...");
                var user = context.Users.Find(userId);
                if (user == null)
                {
                    Console.WriteLine($" Пользователь {userId} не найден");
                    return BadRequest("Пользователь не найден");
                }
                Console.WriteLine($" Пользователь найден: {user.Login}");

                Console.WriteLine($" Поиск игры {gameId}...");
                var game = context.Games.Find(gameId);
                if (game == null)
                {
                    Console.WriteLine($" Игра {gameId} не найдена");
                    return BadRequest("Игра не найдена");
                }
                Console.WriteLine($" Игра найдена: {game.Title}");

                Console.WriteLine($" Проверка существующей записи в корзине...");
                var existingCartItem = context.Carts
                    .FirstOrDefault(c => c.UserId == userId && c.GameId == gameId);

                if (existingCartItem != null)
                {
                    Console.WriteLine($" Обновление существующей записи...");
                    existingCartItem.Quantity += quantity;
                    context.SaveChanges();
                    Console.WriteLine($" Запись обновлена");
                    return Ok(existingCartItem);
                }

                Console.WriteLine($" Создание новой записи...");
                Cart cartItem = new Cart
                {
                    UserId = userId,
                    GameId = gameId,
                    Quantity = quantity
                };

                Console.WriteLine($"Добавление в контекст...");
                context.Carts.Add(cartItem);

                Console.WriteLine($" Сохранение изменений...");
                context.SaveChanges();

                Console.WriteLine($" Создана запись с ID: {cartItem.Id}");
                return Ok(cartItem);
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"  ERROR: {dbEx.Message}");
                Console.WriteLine($" EXCEPTION: {dbEx.InnerException?.Message}");
                Console.WriteLine($"  TYPE: {dbEx.InnerException?.GetType()}");

                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($" FULL INNER: {dbEx.InnerException}");
                }

                return StatusCode(500, $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                Console.WriteLine($" {ex.StackTrace}");
                Console.WriteLine($" ERROR: {ex}");
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
        /// <summary>
        /// Обновить количество товара в корзине
        /// </summary>
        [HttpPut("UpdateQuantity")]
        [ProducesResponseType(typeof(Cart), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult UpdateQuantity([FromForm] int cartId, [FromForm] int quantity)
        {
            try
            {
                using var context = new GameShopContext();

                var cartItem = context.Carts.Find(cartId);
                if (cartItem == null)
                {
                    return NotFound("Запись в корзине не найдена");
                }

                if (quantity <= 0)
                {
                   
                    return BadRequest("Количество должно быть больше 0");
                }

                cartItem.Quantity = quantity;
                context.SaveChanges();

                return Ok(cartItem);
            }
            catch (DbUpdateException dbEx)
            {
               return StatusCode(500, $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
               return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
        /// <summary>
        /// Удалить игру из корзины по ID записи корзины
        /// </summary>
        [HttpDelete("RemoveFromCart/{cartId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult RemoveFromCart(int cartId)
        {
            try
            {
                using var context = new GameShopContext();

                var cartItem = context.Carts.Find(cartId);
                if (cartItem == null)
                    return NotFound("Запись в корзине не найдена");

                context.Carts.Remove(cartItem);
                context.SaveChanges();

                return Ok("Игра удалена из корзины");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в RemoveFromCart: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при удалении из корзины: {ex.Message}");
            }
        }

        /// <summary>
        /// Очистить всю корзину пользователя
        /// </summary>
        [HttpDelete("ClearCart/{userId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult ClearCart(int userId)
        {
            try
            {
                using var context = new GameShopContext();

                var cartItems = context.Carts.Where(c => c.UserId == userId).ToList();

                if (cartItems.Any())
                {
                    context.Carts.RemoveRange(cartItems);
                    context.SaveChanges();
                    return Ok("Корзина очищена");
                }

                return Ok("Корзина уже пуста");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ClearCart: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при очистке корзины: {ex.Message}");
            }
        }

        [HttpGet("GetUserCart/{userId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(List<Cart>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetUserCart(int userId)
        {
            try
            {
                using var context = new GameShopContext();

                var cartItems = context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Game)
                    .ToList();

                if (cartItems.Count == 0)
                    return NotFound("Корзина пуста");

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении корзины: {ex.Message}");
            }
        }
        /// <summary>
        /// Получить количество игр в корзине пользователя
        /// </summary>
        [HttpGet("GetCartItemsCount/{userId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetCartItemsCount(int userId)
        {
            try
            {
                using var context = new GameShopContext();

                var totalItems = context.Carts
                    .Where(c => c.UserId == userId)
                    .Sum(c => c.Quantity);

                return Ok(totalItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении количества товаров в корзине: {ex.Message}");
            }
        }
        [HttpGet("TestConnection")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult TestConnection()
        {
            try
            {
                using var context = new GameShopContext();
                var cartCount = context.Carts.Count();
                return Ok(new
                {
                    message = "Соединение с БД установлено",
                    cartItemsCount = cartCount,
                    database = "Game_ShopDB"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка соединения с БД: {ex.Message}");
            }
        }
    }
}