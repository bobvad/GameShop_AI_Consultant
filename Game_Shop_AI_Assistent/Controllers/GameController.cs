using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Game_Shop_AI_Assistent.Controllers
{
    [Route("api/GameController")]
    [ApiExplorerSettings(GroupName = "v2")]
    [ApiController]
    public class GameController : Controller
    {
        /// <summary>
        /// Импорт игр из Excel файла (.xlsx)
        /// </summary>
        [HttpPost("ImportFromExcel")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Поддерживаются только файлы формата .xlsx");

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var context = new GameShopContext();
                var importedGames = new List<Game>();
                var errors = new List<string>();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var title = worksheet.Cells[row, 1].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(title))
                            continue;

                        var existingGame = await context.Games
                            .FirstOrDefaultAsync(g => g.Title.ToLower() == title.ToLower());

                        if (existingGame != null)
                        {
                            errors.Add($"Строка {row}: Игра '{title}' уже существует");
                            continue;
                        }

                        var game = new Game
                        {
                            Title = title,
                            Description = worksheet.Cells[row, 2].Text?.Trim(),
                            Price = decimal.TryParse(worksheet.Cells[row, 3].Text, out var price) ? price : 0,
                            ReleaseDate = DateTime.TryParse(worksheet.Cells[row, 4].Text, out var date) ? date : DateTime.MinValue,
                            Developer = worksheet.Cells[row, 5].Text?.Trim(),
                            Publisher = worksheet.Cells[row, 6].Text?.Trim(),
                            AgeRating = worksheet.Cells[row, 7].Text?.Trim()
                        };

                        if (string.IsNullOrWhiteSpace(game.Title))
                        {
                            errors.Add($"Строка {row}: Не указано название игры");
                            continue;
                        }

                        context.Games.Add(game);
                        importedGames.Add(game);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Строка {row}: Ошибка парсинга - {ex.Message}");
                    }
                }

                if (importedGames.Any())
                {
                    await context.SaveChangesAsync();
                }

                var result = new
                {
                    ImportedCount = importedGames.Count,
                    ErrorsCount = errors.Count,
                    Errors = errors,
                    Games = importedGames
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при импорте: {ex.Message}");
            }
        }

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
        public ActionResult UpdateGame(
            [FromForm] int Id,
            [FromForm] string Title,
            [FromForm] string Description,
            [FromForm] decimal Price,
            [FromForm] DateTime ReleaseDate,
            [FromForm] string Developer,
            [FromForm] string Publisher,
            [FromForm] string AgeRating)
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

        /// <summary>
        /// Удалить игру по ID
        /// </summary>
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [Route("DeleteById")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
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
        /// Удалить все игры из базы данных
        /// </summary>
        [ApiExplorerSettings(GroupName = "v4")]
        [HttpDelete]
        [Route("DeleteByAll")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteByAll()
        {
            try
            {
                using var context = new GameShopContext();
                var allGames = context.Games.ToList();
                context.Games.RemoveRange(allGames);
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