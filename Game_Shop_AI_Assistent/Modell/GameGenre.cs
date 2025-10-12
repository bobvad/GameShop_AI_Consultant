using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Связь между игрой и жанром
/// </summary>
[Table("game_genres")]
public class GameGenre
{
    /// <summary>
    /// Уникальный идентификатор связи
    /// </summary>
    [Key]
    [Column("game_genre_id")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    [Column("game_id")]
    public int GameId { get; set; }

    /// <summary>
    /// Идентификатор жанра
    /// </summary>
    [Column("genre_id")]
    public int GenreId { get; set; }

    /// <summary>
    /// Игра
    /// </summary>
    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Жанр
    /// </summary>
    [ForeignKey("GenreId")]
    public Genre Genre { get; set; } = null!;
}