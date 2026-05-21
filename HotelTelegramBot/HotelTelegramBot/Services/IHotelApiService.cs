using HotelTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Services
{
    public interface IHotelApiService
    {
        Task<string> GetLocationIdAsync(string cityName);
        Task<List<Hotel>> GetHotelsAsync(string locationId);
        Task<List<string>> GetHotelAmenitiesAsync(string hotelId);
        Task<List<string>> GetHotelReviewsAsync(string hotelId);
    }
}
