using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Жанр игры
/// </summary>
[Table("genres")]
public class Genre
{
    /// <summary>
    /// Уникальный идентификатор жанра
    /// </summary>
    [Key]
    [Column("genre_id")]
    public int Id { get; set; }

    /// <summary>
    /// Название жанра
    /// </summary>
    [Column("genre_name")]
    public string GenreName { get; set; } = string.Empty;

    /// <summary>
    /// Коллекция связей жанра с играми
    /// </summary>
    public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
}