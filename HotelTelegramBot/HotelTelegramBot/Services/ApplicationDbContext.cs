using HotelTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Services
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FavoriteHotel> FavoriteHotels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = config.GetConnectionString("DefaultConnection")
                                          ?? config["ConnectionStrings:DefaultConnection"];

                optionsBuilder.UseNpgsql(connectionString);
            }
        }
    }
}
