using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Корзина покупок пользователя
/// </summary>
[Table("cart")]
public class Cart
{
    /// <summary>
    /// Уникальный идентификатор записи в корзине
    /// </summary>
    [Key]
    [Column("cart_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Пользователь, которому принадлежит корзина
    /// </summary>
    [ForeignKey("UserId")]
    public Users User { get; set; } = null!;

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    [Column("game_id")]
    public int GameId { get; set; }

    /// <summary>
    /// Игра в корзине
    /// </summary>
    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Количество товара
    /// </summary>
    [Column("quantity")]
    public int Quantity { get; set; } = 1; 
    /// <summary>
    /// Вычисляемая стоимость
    /// </summary>
    [NotMapped]
    public decimal TotalPrice => Game?.Price * Quantity ?? 0;
}