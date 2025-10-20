using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    /// покупка игр, получение истории покупок, управление ключами активации и т.д.
    /// </remarks>
    public class PurchasesController : Controller
    {
        /// <summary>
        /// Получить покупки конкретного пользователя с названиями игр
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список покупок пользователя с названиями игр</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetUserPurchases/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetUserPurchases(int userId)
        {
            try
            {
                using var context = new GameShopContext();

                var purchases = context.Purchases
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
                    .ToList();

                if (purchases.Count == null)
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

              
                string activationKey = GenerateActivationKey();

                Purchase purchase = new Purchase();
                purchase.UserId = userId;
                purchase.GameId = gameId;
                purchase.PurchaseDate = DateTime.UtcNow;
                purchase.ActivationKey = activationKey; 
                purchase.KeyStatus = "active";

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
        /// <param name="activationKey">Ключ активации (опционально)</param>
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
            [FromForm] DateTime purchaseDate,
            [FromForm] string activationKey = null)
        {
            try
            {
                using var context = new GameShopContext();

                var existingPurchase = context.Purchases
                    .FirstOrDefault(p => p.UserId == userId && p.GameId == gameId);

                if (existingPurchase != null)
                    return Conflict("Игра уже куплена этим пользователем");

                if (string.IsNullOrEmpty(activationKey))
                {
                    activationKey = GenerateActivationKey();
                }

                Purchase purchase = new Purchase();
                purchase.UserId = userId;
                purchase.GameId = gameId;
                purchase.PurchaseDate = purchaseDate;
                purchase.ActivationKey = activationKey;
                purchase.KeyStatus = "active";

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
        /// <returns>Информация о покупке с названием игры</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetPurchase/{purchaseId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetPurchase(int purchaseId)
        {
            try
            {
                using var context = new GameShopContext();
                var purchase = context.Purchases
                    .Where(p => p.Id == purchaseId)
                    .Select(p => new
                    {
                        Purchase = p,
                        GameTitle = p.Game.Title 
                    })
                    .FirstOrDefault();

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

        /// <summary>
        /// Сгенерировать новый ключ активации для покупки
        /// </summary>
        /// <param name="purchaseId">ID покупки</param>
        /// <response code="200">Новый ключ успешно сгенерирован</response>
        /// <response code="404">Покупка не найдена</response>
        /// <response code="500">Ошибка сервера</response>
        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("GenerateNewActivationKey/{purchaseId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GenerateNewActivationKey(int purchaseId)
        {
            try
            {
                using var context = new GameShopContext();
                var purchase = context.Purchases.Find(purchaseId);

                if (purchase == null)
                    return NotFound("Покупка не найдена");

                string newActivationKey = GenerateActivationKey();
                purchase.ActivationKey = newActivationKey;
                purchase.KeyStatus = "active";

                context.SaveChanges();

                return Ok(new
                {
                    NewActivationKey = newActivationKey,
                    Message = "Новый ключ активации успешно сгенерирован"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при генерации нового ключа");
            }
        }
        ///// <summary>
        ///// Удалить все покупки пользователя
        ///// </summary>
        ///// <param name="userId">ID пользователя</param>
        ///// <response code="200">Все покупки пользователя удалены</response>
        ///// <response code="404">Покупки не найдены</response>
        ///// <response code="500">Ошибка сервера при удалении</response>
        //[ApiExplorerSettings(GroupName = "v2")]
        //[HttpDelete("DeleteAllUserPurchases/{userId}")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //[ProducesResponseType(500)]
        //public ActionResult DeleteAllUserPurchases(int userId)
        //{
        //    try
        //    {
        //        using var context = new GameShopContext();

        //        var userPurchases = context.Purchases
        //            .Where(p => p.UserId == userId)
        //            .ToList();

        //        if (userPurchases.Count == 0)
        //            return NotFound("Покупки не найдены");

        //        context.Purchases.RemoveRange(userPurchases);
        //        context.SaveChanges();

        //        return Ok($"Все покупки пользователя (всего {userPurchases.Count}) успешно удалены");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Произошла ошибка при удалении покупок пользователя");
        //    }
        //}

        /// <summary>
        /// Получить покупки по статусу ключа
        /// </summary>
        /// <param name="keyStatus">Статус ключа (active, used, revoked)</param>
        /// <returns>Список покупок с указанным статусом ключа</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetPurchasesByKeyStatus/{keyStatus}")]
        [ProducesResponseType(typeof(List<Purchase>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult GetPurchasesByKeyStatus(string keyStatus)
        {
            try
            {
                if (!new[] { "active", "used", "revoked" }.Contains(keyStatus))
                    return BadRequest("Неверный статус ключа. Допустимые значения: active, used, revoked");

                using var context = new GameShopContext();
                List<Purchase> purchases = context.Purchases
                    .Where(p => p.KeyStatus == keyStatus)
                    .ToList();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка покупок");
            }
        }

        /// <summary>
        /// Генерация уникального ключа активации
        /// </summary>
        /// <returns>Сгенерированный ключ активации</returns>
        private string GenerateActivationKey()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}