using HotelTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelTelegramBot.Services
{
    public class CachedHotelService : IHotelApiService
    {
        private readonly IHotelApiService _innerService;

        private readonly Dictionary<string, (List<Hotel> Hotels, DateTime ExpirationTime)> _hotelsCache = new();
        private readonly Dictionary<string, (string LocationId, DateTime ExpirationTime)> _locationCache = new();
        private readonly Dictionary<string, (List<string> Reviews, DateTime ExpirationTime)> _reviewsCache = new();

        private readonly Dictionary<string, (List<string> Amenities, DateTime ExpirationTime)> _amenitiesCache = new();

        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        public CachedHotelService(IHotelApiService innerService)
        {
            _innerService = innerService;
        }

        public async Task<string> GetLocationIdAsync(string cityName)
        {
            string key = cityName.ToLower();

            if (_locationCache.TryGetValue(key, out var cachedData) && cachedData.ExpirationTime > DateTime.Now)
            {
                Console.WriteLine($"\n[CACHE] ID для міста '{cityName}' взято з кешу");
                return cachedData.LocationId;
            }

            string locationId = await _innerService.GetLocationIdAsync(cityName);

            if (locationId != null)
            {
                _locationCache[key] = (locationId, DateTime.Now.Add(_cacheDuration));
                Console.WriteLine($"\n[CACHE] ID для міста '{cityName}' збережено в кеш");
            }

            return locationId;
        }

        public async Task<List<Hotel>> GetHotelsAsync(string locationId)
        {
            if (_hotelsCache.TryGetValue(locationId, out var cachedData) && cachedData.ExpirationTime > DateTime.Now)
            {
                Console.WriteLine($"[CACHE] Готелі для локації '{locationId}' взято з кешу");
                return cachedData.Hotels;
            }

            var hotels = await _innerService.GetHotelsAsync(locationId);

            if (hotels != null && hotels.Count > 0)
            {
                _hotelsCache[locationId] = (hotels, DateTime.Now.Add(_cacheDuration));
                Console.WriteLine($"[CACHE] Готелі для локації '{locationId}' збережено в кеш");
            }

            return hotels;
        }

        public async Task<List<string>> GetHotelAmenitiesAsync(string hotelId)
        {
            if (_amenitiesCache.TryGetValue(hotelId, out var cachedData) && cachedData.ExpirationTime > DateTime.Now)
            {
                Console.WriteLine($"[CACHE] Зручності для готелю ID '{hotelId}' взято з кешу");
                return cachedData.Amenities;
            }

            var amenities = await _innerService.GetHotelAmenitiesAsync(hotelId);

            if (amenities != null && amenities.Count > 0)
            {
                _amenitiesCache[hotelId] = (amenities, DateTime.Now.Add(_cacheDuration));
                Console.WriteLine($"[CACHE] Зручності для готелю ID '{hotelId}' збережено в кеш");
            }

            return amenities;
        }

        public async Task<List<string>> GetHotelReviewsAsync(string hotelId)
        {
            if (_reviewsCache.TryGetValue(hotelId, out var cachedData) && cachedData.ExpirationTime > DateTime.Now)
            {
                Console.WriteLine($"[CACHE] Відгуки для готелю ID '{hotelId}' взято з кешу");
                return cachedData.Reviews;
            }

            var reviews = await _innerService.GetHotelReviewsAsync(hotelId);

            if (reviews != null && reviews.Count > 0)
            {
                _reviewsCache[hotelId] = (reviews, DateTime.Now.Add(_cacheDuration));
                Console.WriteLine($"[CACHE] Відгуки для готелю ID '{hotelId}' збережено в кеш");
            }

            return reviews;
        }
    }
}