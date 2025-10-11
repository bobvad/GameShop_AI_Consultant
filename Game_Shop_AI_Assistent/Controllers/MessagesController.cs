using GameShop.Context;
using Microsoft.AspNetCore.Mvc;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/MessagesController")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class MessagesController: Controller
    {
            /// <summary>
            /// Получить все отзывы из базы данных
            /// </summary>
            [ApiExplorerSettings(GroupName = "v1")]
            [HttpGet("GetAllMessage")]
            [ProducesResponseType(typeof(List<Message>), 200)]
            [ProducesResponseType(500)]
            public IActionResult GetAllMessage()
            {
                try
                {
                    GameShopContext context = new GameShopContext();
                    List<Message> messages = context.Messages.ToList();
                    return Json(messages);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Ошибка при получении списка отзывов");
                }
            }
    }
}