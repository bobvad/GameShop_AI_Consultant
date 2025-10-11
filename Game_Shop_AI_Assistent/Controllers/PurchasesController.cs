using GameShop.Context;
using Microsoft.AspNetCore.Mvc;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/PurchasesController")]
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    /// <summary>
    /// Контроллер для управления покупками в магазине
    /// </summary>
    /// <remarks>
    /// Этот контроллер предоставляет API для работы с покупками:
    /// покупка игр, получение истории покупок и т.д.
    /// </remarks>
    public class PurchasesController : Controller
    {
        /// <summary>
        /// Получить все покупки из базы данных
        /// </summary>
        /// <returns>Список всех покупок</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetAllPurchase")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetAllPurchase()
        {
            try
            {
                using var context = new GameShopContext();
                List<Purchase> purchases = context.Purchases.ToList();
                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        /// <summary>
        /// Получить покупки конкретного пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список покупок пользователя</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetUserPurchases/{userId}")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetUserPurchases(int userId)
        {
            try
            {
                using var context = new GameShopContext();
                List<Purchase> purchases = context.Purchases
                    .Where(p => p.UserId == userId)
                    .ToList();

                if (purchases.Count == 0)
                    return NotFound("Покупки не найдены");

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        /// <summary>
        /// Купить игру (основной метод для кнопки "Купить")
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="gameId">ID игры</param>
        /// <remarks>Пользователь нажимает кнопку "Купить" и игра добавляется в его покупки</remarks>
        /// <response code="200">Игра успешно куплена</response>
        /// <response code="400">Ошибка в данных</response>
        /// <response code="409">Игра уже куплена</response>
        /// <response code="500">Ошибка сервера при покупке</response>
        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("BuyGame")]
        [ProducesResponseType(typeof(Purchase), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult BuyGame(
            [FromForm] int userId,
            [FromForm] int gameId)
        {
            try
            {
                using var context = new GameShopContext();

                var user = context.Users.Find(userId);
                if (user == null)
                    return BadRequest("Пользователь не найден");

                var game = context.Games.Find(gameId);
                if (game == null)
                    return BadRequest("Игра не найдена");

                var existingPurchase = context.Purchases
                    .FirstOrDefault(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                    return Conflict("Игра уже куплена этим пользователем");

                Purchase purchase = new Purchase();
                purchase.UserId = userId;
                purchase.GameId = gameId;
                purchase.PurchaseDate = DateTime.UtcNow; 

                context.Purchases.Add(purchase);
                context.SaveChanges();

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла ошибка при покупке игры");
            }
        }

        /// <summary>
        /// Добавление новой покупки в магазин
        /// </summary>
        /// <param name="userId">Покупатель</param>
        /// <param name="gameId">Какую игру купил</param>
        /// <param name="purchaseDate">Дата покупки</param>
        /// <remarks>Данный метод добавляет покупку</remarks>
        /// <response code="200">Покупка успешно добавлена</response>
        /// <response code="400">Некорректные данные покупки</response>
        /// <response code="500">Ошибка сервера при добавлении покупки</response>
        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("AddPurchases")]
        [ProducesResponseType(typeof(Purchase), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult AddPurchases(
            [FromForm] int userId,
            [FromForm] int gameId,
            [FromForm] DateTime purchaseDate)
        {
            try
            {
                using var context = new GameShopContext();

                var existingPurchase = context.Purchases
                    .FirstOrDefault(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                    return Conflict("Игра уже куплена этим пользователем");

                Purchase purchase = new Purchase();
                purchase.UserId = userId;
                purchase.GameId = gameId;
                purchase.PurchaseDate = purchaseDate;

                context.Purchases.Add(purchase);
                context.SaveChanges();

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла ошибка при внесении данных о покупке");
            }
        }

        /// <summary>
        /// Получить детальную информацию о покупке
        /// </summary>
        /// <param name="purchaseId">ID покупки</param>
        /// <returns>Информация о покупке</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetPurchase/{purchaseId}")]
        [ProducesResponseType(typeof(Purchase), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetPurchase(int purchaseId)
        {
            try
            {
                using var context = new GameShopContext();
                var purchase = context.Purchases.Find(purchaseId);

                if (purchase == null)
                    return NotFound("Покупка не найдена");

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении информации о покупке");
            }
        }

        /// <summary>
        /// Получить последние покупки пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="count">Количество последних покупок</param>
        /// <returns>Список последних покупок</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetRecentPurchases/{userId}")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetRecentPurchases(int userId, [FromQuery] int count = 5)
        {
            try
            {
                using var context = new GameShopContext();
                List<Purchase> purchases = context.Purchases
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(count)
                    .ToList();

                if (purchases.Count == 0)
                    return NotFound("Покупки не найдены");

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }
    }
}