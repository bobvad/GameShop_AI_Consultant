using Game_Shop_AI_Assistent.Modell;
using GameShop.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class GameKeysController : ControllerBase
{
    private readonly GameShopContext _context;

    public GameKeysController(GameShopContext context)
    {
        _context = context;
    }

    [HttpPost("ImportKeysByGameName")]
    public async Task<ActionResult<ImportResult>> ImportKeysByGameName(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл пустой");

        var result = new ImportResult();
        var keyGamePairs = new List<(string Key, string GameName)>();

        var extension = Path.GetExtension(file.FileName).ToLower();

        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
        {
            var content = await reader.ReadToEndAsync();

            if (extension == ".json")
            {
                var jsonData = System.Text.Json.JsonSerializer.Deserialize<List<KeyImportJson>>(content);

                if (jsonData != null)
                {
                    keyGamePairs.AddRange(jsonData
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.GameTitle))
                        .Select(x => (x.Key.Trim(), x.GameTitle.Trim())));
                }
            }
            else
            {
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string gameName;
                    string key;

                    if (line.Contains('|'))
                    {
                        var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) continue;

                        gameName = parts[0].Trim();
                        key = parts[1].Trim();
                    }
                    else if (line.Contains(','))
                    {
                        var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) continue;

                        gameName = parts[0].Trim();
                        key = parts[1].Trim();
                    }
                    else
                    {
                        continue;
                    }

                    keyGamePairs.Add((key, gameName));
                }
            }
        }

        if (!keyGamePairs.Any())
            return BadRequest("Нет данных для импорта");

        var gameNames = keyGamePairs
            .Select(x => x.GameName.ToLower())
            .Distinct()
            .ToList();

        var games = await _context.Games
            .Where(g => g.Title != null && gameNames.Contains(g.Title.ToLower()))
            .ToListAsync();

        var gameDict = games
            .Where(g => g.Title != null)
            .ToDictionary(g => g.Title!.ToLower(), g => g.Id);

        var keys = keyGamePairs.Select(x => x.Key).ToList();

        var existingKeys = await _context.GameKeys
            .Where(k => keys.Contains(k.Key))
            .Select(k => k.Key)
            .ToHashSetAsync();

        var keysToAdd = new List<GameKeys>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var pair in keyGamePairs)
            {
                var gameKey = pair.GameName.ToLower();

                if (!gameDict.TryGetValue(gameKey, out var gameId))
                {
                    result.Errors.Add($"Игра не найдена: {pair.GameName} для ключа {pair.Key}");
                    result.ErrorsCount++;
                    continue;
                }

                if (existingKeys.Contains(pair.Key))
                {
                    result.Errors.Add($"Ключ уже существует: {pair.Key}");
                    result.ErrorsCount++;
                    continue;
                }

                keysToAdd.Add(new GameKeys
                {
                    GameId = gameId,
                    Key = pair.Key,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });

                existingKeys.Add(pair.Key);
                result.ImportedCount++;
            }

            if (keysToAdd.Any())
            {
                await _context.GameKeys.AddRangeAsync(keysToAdd);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            result.Message = $"Импортировано {result.ImportedCount} ключей, ошибок {result.ErrorsCount}";
            return Ok(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Ошибка импорта: {ex.Message}");
        }
    }
}

#region DTO

public class KeyImportJson
{
    public string GameTitle { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public class ImportResult
{
    public int ImportedCount { get; set; }
    public int ErrorsCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

#endregion