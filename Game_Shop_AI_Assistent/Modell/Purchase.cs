using System;

namespace Game_Shop_AI_Assistent.Modell
{
    public class Purchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GameId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string KeyStatus { get; set; } = "active";
        public string ActivationKey { get; set; }
        public decimal Price { get; set; }
        public Users User { get; set; }
        public Game Game { get; set; }
    }
}