using Game_Shop_AI_Assistent.Modell;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game_Shop_AI_Assistent.Modell
{
    public class Purchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GameId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string KeyStatus { get; set; } = "active";

        public int? GameKeyId { get; set; }
        public GameKeys? GameKey { get; set; }

        public Users User { get; set; } = null!;
        public Game Game { get; set; } = null!;
        public string? ActivationKey { get; set; }
        public string? ActivationKeyValue => GameKey?.Key ?? ActivationKey;

        public DateTime? KeyUsedAt => GameKey?.UsedAt;
    }
}