using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/GameGenreController")]
    [ApiExplorerSettings(GroupName = "v1")]
    /// <summary>
    /// Контроллер для управления связями игр и жанров
    /// </summary>
    /// <remarks>
    /// Этот контроллер предоставляет API для работы со связями между играми и жанрами
    /// </remarks>
    public class GameGenreController : Controller
    {
        /// <summary>
        /// Получение всех связей между играми и жанрами
        /// </summary> 
        /// <remarks>Данный метод получает все связи между играми и жанрами</remarks>
        /// <response code="200">Связи успешно получены</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("GetAllGameGenres")]
        [HttpGet]
        [ProducesResponseType(typeof(List<GameGenre>), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetAllGameGenres()
        {
            try
            {
                var context = new GameShopContext();
                var gameGenres = context.GameGenres.ToList();
                return Json(gameGenres);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавление новой связи между игрой и жанром
        /// </summary>
        /// <param name="gameId">ID игры</param>
        /// <param name="genreId">ID жанра</param>
        /// <remarks>Данный метод добавляет новую связь между игрой и жанром</remarks>
        /// <response code="200">Связь успешно добавлена</response>
        /// <response code="400">Ошибка в данных</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("AddGameGenre")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(typeof(GameGenre), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult AddGameGenre([FromForm] int gameId, [FromForm] int genreId)
        {
            try
            {
                var context = new GameShopContext();

                
                var existing = context.GameGenres
                    .FirstOrDefault(x => x.GameId == gameId && x.GenreId == genreId);

                if (existing != null)
                {
                    return StatusCode(400, "Такая связь уже существует");
                }

                var gameGenre = new GameGenre
                {
                    GameId = gameId,
                    GenreId = genreId
                };

                context.GameGenres.Add(gameGenre);
                context.SaveChanges();

                return Json(gameGenre);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление связи между игрой и жанром
        /// </summary>
        /// <param name="id">ID связи</param>
        /// <param name="gameId">Новый ID игры</param>
        /// <param name="genreId">Новый ID жанра</param>
        /// <remarks>Данный метод обновляет существующую связь между игрой и жанром</remarks>
        /// <response code="200">Связь успешно обновлена</response>
        /// <response code="404">Связь не найдена</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("UpdateGameGenre")]
        [HttpPut]
        [ApiExplorerSettings(GroupName = "v3")]
        [ProducesResponseType(typeof(GameGenre), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult UpdateGameGenre([FromForm] int id, [FromForm] int gameId, [FromForm] int genreId)
        {
            try
            {
                var context = new GameShopContext();
                var gameGenre = context.GameGenres.FirstOrDefault(x => x.Id == id);

                if (gameGenre == null)
                {
                    return StatusCode(404, "Связь не найдена");
                }

                gameGenre.GameId = gameId;
                gameGenre.GenreId = genreId;

                context.GameGenres.Update(gameGenre);
                context.SaveChanges();

                return Json(gameGenre);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление связи между игрой и жанром
        /// </summary>
        /// <param name="id">ID связи</param>
        /// <remarks>Данный метод удаляет связь между игрой и жанром</remarks>
        /// <response code="200">Связь успешно удалена</response>
        /// <response code="404">Связь не найдена</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("DeleteGameGenre")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult DeleteGameGenre([FromForm] int id)
        {
            try
            {
                var context = new GameShopContext();
                var gameGenre = context.GameGenres.FirstOrDefault(x => x.Id == id);

                if (gameGenre == null)
                {
                    return StatusCode(404, "Связь не найдена");
                }

                context.GameGenres.Remove(gameGenre);
                context.SaveChanges();

                return Json("Связь успешно удалена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление связи по ID игры и жанра
        /// </summary>
        /// <param name="gameId">ID игры</param>
        /// <param name="genreId">ID жанра</param>
        /// <remarks>Данный метод удаляет связь по конкретной игре и жанру</remarks>
        /// <response code="200">Связь успешно удалена</response>
        /// <response code="404">Связь не найдена</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("DeleteByGameAndGenre")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult DeleteByGameAndGenre([FromForm] int gameId, [FromForm] int genreId)
        {
            try
            {
                var context = new GameShopContext();
                var gameGenre = context.GameGenres
                    .FirstOrDefault(x => x.GameId == gameId && x.GenreId == genreId);

                if (gameGenre == null)
                {
                    return StatusCode(404, "Связь не найдена");
                }

                context.GameGenres.Remove(gameGenre);
                context.SaveChanges();

                return Json("Связь успешно удалена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }
    }
}