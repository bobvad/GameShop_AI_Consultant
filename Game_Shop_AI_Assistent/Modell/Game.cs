using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Игра в магазине
/// </summary>
[Table("games")]
public class Game
{
    /// <summary>
    /// Уникальный идентификатор игры
    /// </summary>
    [Key]
    [Column("game_id")]
    public int Id { get; set; }

    /// <summary>
    /// Название игры
    /// </summary>
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание игры
    /// </summary>
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Цена игры
    /// </summary>
    [Column("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Дата выхода игры
    /// </summary>
    [Column("release_date")]
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Разработчик игры
    /// </summary>
    [Column("developer")]
    public string Developer { get; set; } = string.Empty;

    /// <summary>
    /// Издатель игры
    /// </summary>
    [Column("publisher")]
    public string Publisher { get; set; } = string.Empty;

    /// <summary>
    /// Возрастной рейтинг игры
    /// </summary>
    [Column("age_rating")]
    public string AgeRating { get; set; } = string.Empty;

    /// <summary>
    /// Коллекция связей игры с жанрами
    /// </summary>
    public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();

    /// <summary>
    /// Коллекция покупок этой игры
    /// </summary>
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    /// <summary>
    /// Коллекция отзывов на эту игру
    /// </summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
