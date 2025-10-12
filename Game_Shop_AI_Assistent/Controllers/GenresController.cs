using GameShop.Context;
using Microsoft.AspNetCore.Mvc;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    /// <summary>
    /// Контроллер для управления жанрами игр
    /// </summary>
    /// <remarks>
    /// Этот контроллер предоставляет API для работы с жанрами игр
    /// </remarks>
    public class GenresController : Controller
    {
        /// <summary>
        /// Получение всех жанров
        /// </summary> 
        /// <remarks>Данный метод получает все жанры из базы данных</remarks>
        /// <response code="200">Жанры успешно получены</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("GetAll")]
        [HttpGet]
        [ProducesResponseType(typeof(List<Genre>), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetAll()
        {
            try
            {
                var context = new GameShopContext();
                var genres = context.Genres.ToList();
                return Json(genres);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Добавление нового жанра
        /// </summary>
        /// <param name="genreName">Название жанра</param>
        /// <remarks>Данный метод добавляет новый жанр в систему</remarks>
        /// <response code="200">Жанр успешно добавлен</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("Add")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(typeof(Genre), 200)]
        [ProducesResponseType(500)]
        public ActionResult Add([FromForm] string genreName)
        {
            try
            {
                var context = new GameShopContext();

                var genre = new Genre
                {
                    GenreName = genreName
                };

                context.Genres.Add(genre);
                context.SaveChanges();

                return Json(genre);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Получение жанра по ID
        /// </summary>
        /// <param name="id">ID жанра</param>
        /// <remarks>Данный метод получает жанр по его идентификатору</remarks>
        /// <response code="200">Жанр успешно найден</response>
        /// <response code="404">Жанр не найден</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("GetById")]
        [HttpGet]
        [ProducesResponseType(typeof(Genre), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult GetById([FromQuery] int id)
        {
            try
            {
                var context = new GameShopContext();
                var genre = context.Genres.First(x => x.Id == id);

                if (genre == null)
                    return StatusCode(404, "Жанр не найден");

                return Json(genre);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Удаление жанра
        /// </summary>
        /// <param name="id">ID жанра</param>
        /// <remarks>Данный метод удаляет жанр из системы</remarks>
        /// <response code="200">Жанр успешно удален</response>
        /// <response code="404">Жанр не найден</response>
        /// <response code="500">При выполнении запроса возникли ошибки</response>
        [Route("Delete")]
        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult Delete([FromForm] int id)
        {
            try
            {
                var context = new GameShopContext();
                var genre = context.Genres.FirstOrDefault(x => x.Id == id);

                if (genre == null)
                    return StatusCode(404, "Жанр не найден");

                context.Genres.Remove(genre);
                context.SaveChanges();

                return Json("Жанр успешно удален");
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}