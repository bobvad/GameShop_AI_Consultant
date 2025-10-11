using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Отзыв на игру
/// </summary>
[Table("reviews")]
public class Review
{
    /// <summary>
    /// Уникальный идентификатор отзыва
    /// </summary>
    [Key]
    [Column("review_id")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, оставившего отзыв
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    [Column("game_id")]
    public int GameId { get; set; }

    /// <summary>
    /// Текст отзыва
    /// </summary>
    [Column("review_text")]
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания отзыва
    /// </summary>
    [Column("review_date")]
    public DateTime ReviewDate { get; set; }

    /// <summary>
    /// Пользователь, оставивший отзыв
    /// </summary>
    public Users User { get; set; } = null!;

    /// <summary>
    /// Игра, на которую оставлен отзыв
    /// </summary>
    public Game Game { get; set; } = null!;
}