using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : Controller
    {
        /// <summary>
        /// Получить все отзывы из базы данных
        /// </summary>
        [ApiExplorerSettings(GroupName = "v1")]
        [HttpGet("GetAllReview")]
        [ProducesResponseType(typeof(List<Review>), 200)]
        [ProducesResponseType(500)]
        public IActionResult GetAllReview()
        {
            try
            {
                GameShopContext context = new GameShopContext();
                List<Review> reviews = context.Reviews.ToList(); 
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при получении списка отзывов");
            }
        }

        /// <summary>
        /// Добавление нового отзыва к игре
        /// </summary>
        [ApiExplorerSettings(GroupName = "v2")]
        [HttpPost("AddReview")]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public IActionResult AddReview([FromForm] int userId, [FromForm] int gameId, [FromForm] string reviewText)
        {
            try
            {
                using var context = new GameShopContext();

                var userExists = context.Users.Where(u => u.Id == userId);
                var gameExists = context.Games.Where(g => g.Id == gameId);
                var review = new Review
                {
                    UserId = userId,
                    GameId = gameId,
                    ReviewText = reviewText.Trim(),
                    ReviewDate = DateTime.UtcNow
                };
                context.Reviews.Add(review);
                context.SaveChanges();

                return Ok(review);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Не удалось добавить отзыв: {ex.Message}");
            }
        }
    }
}