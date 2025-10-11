using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Покупка игры
/// </summary>
[Table("purchases")]
public class Purchase
{
    /// <summary>
    /// Уникальный идентификатор покупки
    /// </summary>
    [Key]
    [Column("purchase_id")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, совершившего покупку
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор купленной игры
    /// </summary>
    [Column("game_id")]
    public int GameId { get; set; }

    /// <summary>
    /// Дата и время покупки
    /// </summary>
    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Пользователь, совершивший покупку
    /// </summary>
    public Users User { get; set; } = null!;

    /// <summary>
    /// Купленная игра
    /// </summary>
    public Game Game { get; set; } = null!;
}