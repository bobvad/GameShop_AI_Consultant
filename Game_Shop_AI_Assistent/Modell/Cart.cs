using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Корзина покупок пользователя
/// </summary>
public class Cart
{
    /// <summary>
    /// Уникальный идентификатор записи в корзине
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Пользователь, которому принадлежит корзина
    /// </summary>
    public Users User { get; set; } = null!;

    /// <summary>
    /// Идентификатор игры
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Игра в корзине
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Количество товара
    /// </summary>
    public int Quantity { get; set; } = 1; 
    /// <summary>
    /// Вычисляемая стоимость
    /// </summary>
    public decimal TotalPrice => Game?.Price * Quantity ?? 0;
}