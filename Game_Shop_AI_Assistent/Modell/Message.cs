using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Сообщение в системе
/// </summary>
[Table("messages")]
public class Message
{
    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    [Key]
    [Column("message_id")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    [Column("message_text")]
    public string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// Флаг, указывающий отправлено ли сообщение от гостя
    /// </summary>
    [Column("is_from_guest")]
    public bool IsFromGuest { get; set; }

    /// <summary>
    /// Дата и время отправки сообщения
    /// </summary>
    [Column("message_date")]
    public DateTime MessageDate { get; set; }

    /// <summary>
    /// Пользователь, отправивший сообщение
    /// </summary>
    public Users User { get; set; } = null!;
}