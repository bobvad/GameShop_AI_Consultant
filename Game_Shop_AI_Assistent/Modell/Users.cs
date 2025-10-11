using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Пользователь системы
/// </summary>
[Table("users")]
public class Users
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    [Key]
    [Column("user_id")]
    public int Id { get; set; }

    /// <summary>
    /// Имя пользователя (логин)
    /// </summary>
    [Column("user_name")]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта пользователя
    /// </summary>
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя
    /// </summary>
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания аккаунта
    /// </summary>
    [Column("date_time")]
    public DateTime DateTimeCreated { get; set; }

    /// <summary>
    /// Флаг, указывающий является ли пользователь гостем
    /// </summary>
    [Column("is_guest")]
    public bool IsGuest { get; set; }

    /// <summary>
    /// Коллекция покупок пользователя
    /// </summary>
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    /// <summary>
    /// Коллекция отзывов пользователя
    /// </summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>
    /// Коллекция сообщений пользователя
    /// </summary>
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}