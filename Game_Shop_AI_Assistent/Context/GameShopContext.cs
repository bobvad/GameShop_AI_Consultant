using Game_Shop_AI_Assistent.Modell;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
namespace GameShop.Context
{
    public class GameShopContext : DbContext
    {
        public GameShopContext() : base()
        {
            Database.EnsureCreated();
        }

        public GameShopContext(DbContextOptions<GameShopContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<GameGenre> GameGenres { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Cart> Carts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(
                    "server=127.0.0.1;port=3306;uid=root;pwd=;database=Game_ShopDB;Connection Timeout=30;Pooling=true;",
                    new MySqlServerVersion(new Version(8, 1, 0)),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );
                    }
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Здесь ваши конфигурации моделей
        }
    }
}