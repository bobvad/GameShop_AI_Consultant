using GameShop.Context;
using Microsoft.AspNetCore.Mvc;

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
        /// <param name="title">Название игры</param>
        /// <param name="description">Описание игры</param>
        /// <param name="price">Цена игры</param>
        /// <param name="releaseDate">Дата выхода игры</param>
        /// <param name="developer">Разработчик игры</param>
        /// <param name="publisher">Издатель игры</param>
        /// <param name="ageRating">Возрастной рейтинг</param>
        /// <remarks>Данный метод добавляет новую игру в каталог магазина</remarks>
        /// <response code="200">Игра успешно добавлена</response>
        /// <response code="400">Некорректные данные игры</response>
        /// <response code="409">Игра с таким названием уже существует</response>
        /// <response code="500">Ошибка сервера при добавлении игры</response>
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
                var context = new GameShopContext();
                {
                    Game games = new Game();
                    games.Title = title;
                    games.Description = description;
                    games.Price = price;
                    games.ReleaseDate = releaseDate;
                    games.Developer = developer;
                    games.Publisher = publisher;
                    games.AgeRating = ageRating;
                    context.Games.Add(games);
                    context.SaveChanges();
                    return Json(games);
                };
             }
             catch
             {
                return StatusCode(500, "Произошла ошибка при внесении данных об игре");
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
        /// <param name="id">ID игры для изменения</param>
        /// <param name="title">Новое название игры</param>
        /// <param name="description">Новое описание игры</param>
        /// <param name="price">Новая цена игры</param>
        /// <param name="releaseDate">Новая дата выхода игры</param>
        /// <param name="developer">Новый разработчик игры</param>
        /// <param name="publisher">Новый издатель игры</param>
        /// <param name="ageRating">Новый возрастной рейтинг</param>
        /// <remarks>Данный метод обновляет информацию об игре в каталоге магазина</remarks>
        /// <response code="200">Игра успешно изменена</response>
        /// <response code="400">Некорректные данные игры</response>
        /// <response code="404">Игра не найдена</response>
        /// <response code="500">Ошибка сервера при изменении игры</response>
        [ApiExplorerSettings(GroupName = "v3")]
        [HttpPut("UpdateGame")]
        [ProducesResponseType(typeof(Game), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult UpdateGame(
            [FromForm] int id,
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
                {
                    var game = context.Games.Where(g => g.Id == id).First();

                    if (game != null)
                    {
                        game.Title = title;
                        game.Description = description;
                        game.Price = price;
                        game.ReleaseDate = releaseDate;
                        game.Developer = developer;
                        game.Publisher = publisher;
                        game.AgeRating = ageRating;

                        context.SaveChanges();
                        return Ok(game);
                    }
                    else
                    {
                        return NotFound("Игра с указанным ID не найдена"); 
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла ошибка при изменении данных об игре");
            }
        }
        /// <summary>
        /// Метод удаления задачи
        /// </summary> 
        /// <param name="Id">Код задачи</param>
        /// <remarks>Данный метод удаляет задачу в базе данных</remarks>
        ///<response code="200">Задача успешно удалена</response>
        ///<response code="500">При выполнении запроса возникли ошибки</response>
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [Route("DeleteById")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteById(int id)
        {
            try
            {
                GameShopContext context = new GameShopContext();
                Game task = context.Games.Where(x => x.Id == id).First();
                context.Games.Remove(task);
                context.SaveChanges();
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
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