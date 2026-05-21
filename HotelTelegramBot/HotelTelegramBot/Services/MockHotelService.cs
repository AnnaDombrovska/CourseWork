using HotelTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Services
{
    public class MockHotelService : IHotelApiService
    {
        public Task<string> GetLocationIdAsync(string cityName)
        {
            return Task.FromResult("999999");
        }

        public Task<List<Hotel>> GetHotelsAsync(string locationId)
        {
            var mockHotels = new List<Hotel>
            {
                new Hotel {
                    Id = "111",
                    Name = "Grand Hotel Lviv",
                    Price = "$150",
                    Rating = "5.0",
                    Url = "https://www.tripadvisor.com/Search?q=Grand+Hotel+Lviv",
                    ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800"
                },
                new Hotel {
                    Id = "222",
                    Name = "Bankhotel",
                    Price = "$180",
                    Rating = "4.9",
                    Url = "https://www.tripadvisor.com/Search?q=Bankhotel+Lviv",
                    ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800"
                },
                new Hotel {
                    Id = "333",
                    Name = "Astoria Hotel",
                    Price = "$120",
                    Rating = "4.8",
                    Url = "https://www.tripadvisor.com/Search?q=Astoria+Hotel+Lviv",
                    ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c0d1398c?w=800"
                }
            };

            return Task.FromResult(mockHotels);
        }

        public Task<List<string>> GetHotelAmenitiesAsync(string hotelId)
        {
            var mockAmenities = new List<string>
            {
                "Безкоштовний Wi-Fi",
                "Спа та оздоровчий центр",
                "Басейн",
                "Безкоштовна парковка",
                "Ресторан та Бар",
                "Сніданок (шведський стіл)"
            };

            return Task.FromResult(mockAmenities);
        }

        public Task<List<string>> GetHotelReviewsAsync(string hotelId)
        {
            var fakeReviews = new List<string>
            {
                "💬 *Чудовий готель!*\n_Все було супер, дуже чисті номери і смачні сніданки._",
                "💬 *Непогано*\n_Гарне розташування, але Wi-Fi міг би бути швидшим._"
            };
            return Task.FromResult(fakeReviews);
        }
    }
}
