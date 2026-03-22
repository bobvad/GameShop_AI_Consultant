using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Связь между игрой и жанром
/// </summary>
public class GameGenre
{
    /// <summary>
    /// Уникальный идентификатор связи
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Идентификатор жанра
    /// </summary>
    public int GenreId { get; set; }

    /// <summary>
    /// Игра
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Жанр
    /// </summary>
    public Genre Genre { get; set; } = null!;
}