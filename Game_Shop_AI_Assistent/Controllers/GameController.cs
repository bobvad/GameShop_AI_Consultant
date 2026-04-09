using ClosedXML.Excel;
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
        private readonly GameShopContext _context;
        private readonly ILogger<GameController> _logger;

        public GameController(GameShopContext context, ILogger<GameController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("ImportFromExcel")]
        public async Task<ActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не выбран" });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Поддерживаются только файлы .xlsx" });

            try
            {
                var importedGames = new List<Game>();
                var errors = new List<string>();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var lastRow = worksheet.LastRowUsed();
                int rowCount = lastRow?.RowNumber() ?? 1;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var title = worksheet.Cell(row, 1).GetValue<string>()?.Trim();
                        if (string.IsNullOrWhiteSpace(title)) continue;

                        var existingGame = await _context.Games
                            .FirstOrDefaultAsync(g => g.Title.ToLower() == title.ToLower());

                        if (existingGame != null)
                        {
                            errors.Add($"Строка {row}: Игра '{title}' уже существует");
                            continue;
                        }

                        var game = new Game
                        {
                            Title = title,
                            Description = worksheet.Cell(row, 2).GetValue<string>()?.Trim(),
                            Price = worksheet.Cell(row, 3).TryGetValue<decimal>(out var price) ? price : 0,
                            ReleaseDate = worksheet.Cell(row, 4).TryGetValue<DateTime>(out var date) ? date : DateTime.MinValue,
                            Developer = worksheet.Cell(row, 5).GetValue<string>()?.Trim(),
                            Publisher = worksheet.Cell(row, 6).GetValue<string>()?.Trim(),
                            AgeRating = worksheet.Cell(row, 7).GetValue<string>()?.Trim(),
                            Platform = worksheet.Cell(row, 8).GetValue<string>()?.Trim() ?? "Steam",
                            ImageUrl = worksheet.Cell(row, 9).GetValue<string>()?.Trim() ?? ""
                        };

                        _context.Games.Add(game);
                        importedGames.Add(game);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Строка {row}: {ex.Message}");
                    }
                }

                if (importedGames.Any())
                    await _context.SaveChangesAsync();

                return Ok(new
                {
                    ImportedCount = importedGames.Count,
                    ErrorsCount = errors.Count,
                    Errors = errors,
                    Games = importedGames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка импорта Excel");
                return StatusCode(500, new { message = $"Ошибка импорта: {ex.Message}" });
            }
        }

        /// <summary>
        /// Добавление новой игры в магазин
        /// </summary>
        [HttpPost("AddGame")]
        public ActionResult AddGame(
     [FromForm] string title,
     [FromForm] string description,
     [FromForm] decimal price,
     [FromForm] DateTime releaseDate,
     [FromForm] string developer,
     [FromForm] string publisher,
     [FromForm] string ageRating,
     [FromForm] string platform = "Steam",
     [FromForm] string imageUrl = "")
        {
            try
            {
                using var context = new GameShopContext();

                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new { message = "Название игры обязательно" });
                }

                var existingGame = context.Games.FirstOrDefault(g => g.Title.ToLower() == title.ToLower());
                if (existingGame != null)
                {
                    return Conflict(new { message = "Игра с таким названием уже существует" });
                }

                var game = new Game
                {
                    Title = title.Trim(),
                    Description = description ?? "",
                    Price = price,
                    ReleaseDate = releaseDate == DateTime.MinValue ? DateTime.UtcNow : releaseDate,
                    Developer = developer ?? "",
                    Publisher = publisher ?? "",
                    AgeRating = ageRating ?? "",
                    Platform = string.IsNullOrWhiteSpace(platform) ? "Steam" : platform.Trim(),
                    ImageUrl = imageUrl ?? ""
                };

                context.Games.Add(game);
                context.SaveChanges();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
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
                return StatusCode(500, new { message = "Ошибка при получении списка игр" });
            }
        }

        /// <summary>
        /// Изменить игру в базе данных
        /// </summary>
        [ApiExplorerSettings(GroupName = "v3")]
        [HttpPut("UpdateGame")]
        public ActionResult UpdateGame(
    [FromForm] int id,
    [FromForm] string title,
    [FromForm] string description,
    [FromForm] decimal price,
    [FromForm] DateTime releaseDate,
    [FromForm] string developer,
    [FromForm] string publisher,
    [FromForm] string ageRating,
    [FromForm] string platform = "Steam",
    [FromForm] string imageUrl = "")
        {
            try
            {
                using var context = new GameShopContext();
                var game = context.Games.FirstOrDefault(g => g.Id == id);

                if (game == null)
                {
                    return NotFound(new { message = "Игра не найдена" });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new { message = "Название игры обязательно" });
                }

                var duplicateGame = context.Games.FirstOrDefault(g => g.Title.ToLower() == title.ToLower() && g.Id != id);
                if (duplicateGame != null)
                {
                    return Conflict(new { message = "Игра с таким названием уже существует" });
                }

                game.Title = title.Trim();
                game.Description = description ?? "";
                game.Price = price;
                game.ReleaseDate = releaseDate == DateTime.MinValue ? game.ReleaseDate : releaseDate;
                game.Developer = developer ?? "";
                game.Publisher = publisher ?? "";
                game.AgeRating = ageRating ?? "";
                game.Platform = string.IsNullOrWhiteSpace(platform) ? "Steam" : platform.Trim();
                game.ImageUrl = imageUrl ?? "";

                context.SaveChanges();
                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
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
                    return NotFound(new { message = "Игра не найдена" });

                if (game.GameGenres != null && game.GameGenres.Any())
                    context.GameGenres.RemoveRange(game.GameGenres);

                if (game.Purchases != null && game.Purchases.Any())
                    context.Purchases.RemoveRange(game.Purchases);

                if (game.Reviews != null && game.Reviews.Any())
                    context.Reviews.RemoveRange(game.Reviews);

                context.Games.Remove(game);
                context.SaveChanges();

                return Ok(new { message = "Игра успешно удалена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при удалении: {ex.Message}" });
            }
        }

        [HttpGet("GetByGameId/{gameId}")]
        public IActionResult GetByGameId(int gameId)
        {
            try
            {
                using var context = new GameShopContext();
                var reviews = context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.GameId == gameId)
                    .ToList();
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet("GetById/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                using var context = new GameShopContext();
                var game = context.Games.FirstOrDefault(g => g.Id == id);

                if (game == null)
                    return NotFound(new { message = "Игра не найдена" });

                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
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
                return Ok(new { message = "Все игры удалены" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}