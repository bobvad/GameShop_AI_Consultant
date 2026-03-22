/// <summary>
/// Отзыв на игру
/// </summary>
public class Review
{
    /// <summary>
    /// Уникальный идентификатор отзыва
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, оставившего отзыв
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Текст отзыва
    /// </summary>
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания отзыва
    /// </summary>
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