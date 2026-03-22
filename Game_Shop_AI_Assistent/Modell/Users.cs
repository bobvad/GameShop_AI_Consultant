using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Пользователь системы
/// </summary>
public class Users
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя пользователя (логин)
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта пользователя
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания аккаунта
    /// </summary>
    public DateTime DateTimeCreated { get; set; }

    /// <summary>
    /// Флаг, указывающий является ли пользователь гостем
    /// </summary>
    public bool IsGuest { get; set; }

    /// <summary>
    /// Роль пользователя (Admin/User)
    /// </summary>
    public string Role { get; set; } = "User";

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