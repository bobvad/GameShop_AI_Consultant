using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Покупка игры
/// </summary>
public class Purchase
{
    /// <summary>
    /// Уникальный идентификатор покупки
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, совершившего покупку
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Идентификатор купленной игры
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Дата и время покупки
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Ключ активации игры
    /// </summary>
    public string? ActivationKey { get; set; }

    /// <summary>
    /// Статус ключа активации
    /// </summary>
    public string KeyStatus { get; set; } = "active";

    /// <summary>
    /// Пользователь, совершивший покупку
    /// </summary>
    public Users User { get; set; } = null!;

    /// <summary>
    /// Купленная игра
    /// </summary>
    public Game Game { get; set; } = null!;
}