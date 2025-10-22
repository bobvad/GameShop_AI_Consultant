using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/GameController")]
    [ApiExplorerSettings(GroupName = "v2")]
    [ApiController]
    /// <summary>
    /// Контроллер для управления играми в магазине
    /// </summary>
    /// <remarks>
    /// <remarks name="GameController"></remarks>
    /// Этот контроллер предоставляет API для работы с играми:
    /// добавление, изменение, получение информации об играх и т.д.
    /// </remarks>
    // Базовый класс контроллера для обработки HTTP запросов
    public class GameController : Controller
    {
        /// <summary>
        /// Добавление новой игры в магазин
        /// </summary>
        [HttpPost("AddGame")]
        [ProducesResponseType(typeof(Game), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult AddGame(
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] decimal price,
            [FromForm] DateTime releaseDate,
            [FromForm] string developer,
            [FromForm] string publisher,
            [FromForm] string ageRating)
        {
            try
            {
                using var context = new GameShopContext();

                var existingGame = context.Games.FirstOrDefault(g => g.Title.ToLower() == title.ToLower());
                if (existingGame != null)
                {
                    return StatusCode(409, "Игра с таким названием уже существует");
                }

                var game = new Game
                {
                    Title = title,
                    Description = description,
                    Price = price,
                    ReleaseDate = releaseDate,
                    Developer = developer,
                    Publisher = publisher,
                    AgeRating = ageRating
                };

                context.Games.Add(game);
                context.SaveChanges();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка при добавлении игры: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить все игры из базы данных
        /// </summary>
        /// <returns>Список всех игр</returns>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetAllGames")]
        [ProducesResponseType(typeof(List<Game>), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetAllGames()
        {
            try
            {
                using var context = new GameShopContext();
                List<Game> games = context.Games.ToList();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка игр");
            }
        }
        /// <summary>
        /// Изменить игру в базе данных
        /// </summary>
        [ApiExplorerSettings(GroupName = "v3")]
        [HttpPut("UpdateGame")]
        [ProducesResponseType(typeof(Game), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult UpdateGame([FromForm] int Id, [FromForm] string Title, [FromForm] string Description,
            [FromForm] decimal Price, [FromForm] DateTime ReleaseDate, [FromForm] string Developer,
            [FromForm] string Publisher, [FromForm] string AgeRating)
        {
            try
            {
                using var context = new GameShopContext();
                var game = context.Games.FirstOrDefault(g => g.Id == Id);

                if (game == null)
                {
                    return NotFound("Игра с указанным ID не найдена");
                }

                var duplicateGame = context.Games.FirstOrDefault(g => g.Title == Title && g.Id != Id);
                if (duplicateGame != null)
                {
                    return Conflict("Игра с таким названием уже существует");
                }

                game.Title = Title;
                game.Description = Description;
                game.Price = Price;
                game.ReleaseDate = ReleaseDate;
                game.Developer = Developer;
                game.Publisher = Publisher;
                game.AgeRating = AgeRating;

                context.SaveChanges();
                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка при изменении данных об игре: {ex.Message}");
            }
        }
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [Route("DeleteById")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteById(int id)
        {
            try
            {
                using var context = new GameShopContext();
                var game = context.Games
                    .Include(g => g.GameGenres)
                    .Include(g => g.Purchases)
                    .Include(g => g.Reviews)
                    .FirstOrDefault(x => x.Id == id);

                if (game == null)
                    return NotFound("Игра не найдена");

                context.GameGenres.RemoveRange(game.GameGenres);
                context.Purchases.RemoveRange(game.Purchases);
                context.Reviews.RemoveRange(game.Reviews);

                context.Games.Remove(game);
                context.SaveChanges();

                return Ok("Игра успешно удалена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении: {ex.Message}");
            }
        }
        /// <summary>
        /// Метод удаления задачи
        /// </summary> 
        /// <param name="">Код задачи</param>
        /// <remarks>Данный метод удаляет задачу в базе данных</remarks>
        ///<response code="200">Задача успешно удалена</response>
        ///<response code="500">При выполнении запроса возникли ошибки</response>
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [Route("DeleteByAll")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteByAll()
        {
            try
            {
                GameShopContext context = new GameShopContext();
                var allTasks = context.Games.ToList();
                context.Games.RemoveRange(allTasks);
                context.SaveChanges();
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}