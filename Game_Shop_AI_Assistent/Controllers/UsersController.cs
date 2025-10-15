using GameShop.Context;
using Microsoft.AspNetCore.Mvc;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/UserController")]
    [ApiExplorerSettings(GroupName = "v2")]
    /// <summary>
    /// Контроллер для управления пользователями игрового магазина
    /// </summary>
    /// <remarks>
    /// Этот контроллер предоставляет API для работы с пользователями:
    /// регистрация, аутентификация, управление профилями и т.д.
    /// </remarks>
    public class UsersController : Controller
    {
        /// <summary>
        /// Авторизация пользователя
        /// </summary> 
        /// <param name="Login">Логин пользователя</param>
        /// <param name="Password">Пароль пользователя</param>
        /// <remarks>Данный метод получает список задач, по предоставленной данные</remarks>
        ///<response code="200">Пользователя успешно авторизован</response>
        ///<response code = "403">Запрос не имеет данных для авторизации</response>
        ///<response code="500">При выполнении запроса на стороне сервера возникли ошибки</response>
        [Route("SingIn")]
        [HttpPost]
        [ProducesResponseType(typeof(Users), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public ActionResult SingIn([FromForm] string Login, [FromForm] string Password)
        {
            if (Login == null && Password == null)
                return StatusCode(403);
            try
            {
                Users user = new GameShopContext().Users.Where(x => x.Login == Login && x.Password == Password).First();
                return Json(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
        /// <summary>
        /// Регистрация пользователя
        /// </summary> 
        /// <param name="Login">Логин пользователя</param>
        /// <param name="Email">Почта пользователя</param>
        /// <param name="Password">Пароль пользователя</param>
        /// <param name="DateTimeCreated">Создание аккаунта пользователя</param>
        /// <remarks>Данный метод регистрирует нового пользователя в системе</remarks>
        /// <response code="200">Пользователь успешно зарегистрирован</response>
        ///<response code = "403">Запрос не имеет данных для регистрации</response>
        ///<response code="500">При выполнении запроса на стороне сервера возникли ошибки</response>
        [Route("RegIn")]
        [HttpPost]
        [ProducesResponseType(typeof(Users), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult RegIn([FromForm] string Login, [FromForm] string Email, [FromForm] string Password, [FromForm] DateTime DateTimeCreated)
        {
            try
            {
                var context = new GameShopContext();
                {
                    if (context.Users.Any(u => u.Login == Login))
                        return StatusCode(409, "Пользователь с таким логином уже существует");

                    Users user = new Users()
                    {
                        Login = Login.Trim(),
                        Email = Email.Trim(),
                        Password = Password,
                        DateTimeCreated = DateTime.UtcNow,
                        IsGuest = false
                    };
                    context.Users.Add(user);
                    context.SaveChanges();
                    return Json(user);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла ошибка при регистрации пользователя");
            }
        }
        [Route("DeleteById")]
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult DeleteById([FromForm] int id)
        {
            try
            {
                using var context = new GameShopContext();

                var user = context.Users.Where(x => x.Id == id).First();

                if (user == null)
                {
                    return NotFound($"Пользователь с ID {id} не найден");
                }

                context.Users.Remove(user);
                context.SaveChanges();

                return Ok($"Пользователь с ID {id} успешно удален");
            }
            catch (Exception ex)
            {

                return StatusCode(500, "Произошла ошибка при удалении пользователя");
            }
        }
        /// <summary>
        /// Регистрация администратора
        /// </summary> 
        /// <param name="Login">Логин администратора</param>
        /// <param name="Email">Почта администратора</param>
        /// <param name="Password">Пароль администратора</param>
        /// <remarks>Данный метод регистрирует нового администратора в системе</remarks>
        /// <response code="200">Администратор успешно зарегистрирован</response>
        /// <response code="409">Администратор с таким логином уже существует</response>
        /// <response code="500">При выполнении запроса на стороне сервера возникли ошибки</response>
        [Route("RegInAdmin")]
        [HttpPost]
        [ProducesResponseType(typeof(Users), 200)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public ActionResult RegInAdmin([FromForm] string Login, [FromForm] string Email, [FromForm] string Password)
        {
            try
            {
                using var context = new GameShopContext();

                if (context.Users.Any(u => u.Login == Login))
                    return StatusCode(409, "Администратор с таким логином уже существует");

                Users user = new Users()
                {
                    Login = Login.Trim(),
                    Email = Email.Trim(),
                    Password = Password,
                    Role = "Admin",
                    DateTimeCreated = DateTime.UtcNow,
                    IsGuest = false
                };

                context.Users.Add(user);
                context.SaveChanges();
                return Json(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Произошла ошибка при регистрации администратора");
            }
        }
    }
}