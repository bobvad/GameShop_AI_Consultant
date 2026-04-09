using Game_Shop_AI_Assistent.Modell;

public class Game
{
    public int Id { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public string Platform { get; set; } = "Steam";
    public string? ImageUrl { get; set; } = string.Empty;

    public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}