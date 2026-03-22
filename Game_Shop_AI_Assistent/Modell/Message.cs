using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Сообщение в системе
/// </summary>
public class Message
{
    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// Флаг, указывающий отправлено ли сообщение от гостя
    /// </summary>
    public bool IsFromGuest { get; set; }
    public bool IsFromBot { get; set; }
    /// <summary>
    /// Дата и время отправки сообщения
    /// </summary>
    public DateTime MessageDate { get; set; }

    /// <summary>
    /// Пользователь, отправивший сообщение
    /// </summary>
    public Users User { get; set; } = null!;
}