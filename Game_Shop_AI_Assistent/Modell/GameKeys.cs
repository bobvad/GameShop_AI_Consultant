using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game_Shop_AI_Assistent.Modell
{
    public class GameKeys
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        public string Key { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public int? UsedByUserId { get; set; }
        public Users? UsedByUser { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}