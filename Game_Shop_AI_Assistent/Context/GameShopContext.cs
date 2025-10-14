using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace GameShop.Context
{
    public class GameShopContext : DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<GameGenre> GameGenres { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public GameShopContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=192.168.0.9;Port=5432;Database=Game_ShopDB;Username=vova;Password=123");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>().ToTable("users");
            modelBuilder.Entity<Game>().ToTable("games");
            modelBuilder.Entity<Genre>().ToTable("genres");
            modelBuilder.Entity<GameGenre>().ToTable("game_genres");
            modelBuilder.Entity<Purchase>().ToTable("purchases");
            modelBuilder.Entity<Review>().ToTable("reviews");
            modelBuilder.Entity<Message>().ToTable("messages");
            modelBuilder.Entity<Cart>().ToTable("cart"); 

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd()
                      .HasColumnName("cart_id");

                entity.HasIndex(e => new { e.UserId, e.GameId })
                      .IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}