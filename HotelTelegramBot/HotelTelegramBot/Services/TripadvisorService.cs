using HotelTelegramBot.Models;
using HotelTelegramBot.Models.ApiResponses;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelTelegramBot.Services
{
    public class TripadvisorService : IHotelApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiHost;

        public TripadvisorService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _apiKey = config["RapidApi:Key"];
            _apiHost = config["RapidApi:Host"];
        }

        public async Task<string> GetLocationIdAsync(string cityName)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/searchLocation?query={cityName}"),
                Headers =
                {
                    { "x-rapidapi-key", _apiKey },
                    { "x-rapidapi-host", _apiHost },
                }
            };

            try
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); 

                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n[ВІДПОВІДЬ СЕРВЕРА ПРО МІСТО]:");
                Console.WriteLine(body);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var locationResponse = JsonSerializer.Deserialize<LocationResponse>(body, options);

                if (locationResponse?.Data != null && locationResponse.Data.Count > 0)
                {
                    return locationResponse.Data[0].GeoId.ToString();
                }

                return null; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при запиті до API: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Hotel>> GetHotelsAsync(string locationId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/searchHotels?geoId={locationId}&checkIn=2026-06-01&checkOut=2026-06-05"),
                Headers =
                {
                    { "x-rapidapi-key", _apiKey },
                    { "x-rapidapi-host", _apiHost },
                }
            };

            try
            {
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                Console.WriteLine("\nВІДПОВІДЬ СЕРВЕРА");
                Console.WriteLine(body.Substring(0, Math.Min(body.Length, 400)));

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var hotelResponse = JsonSerializer.Deserialize<HotelSearchResponse>(body, options);

                var resultList = new List<Hotel>();

                if (hotelResponse?.Data?.Data != null)
                {
                    foreach (var item in hotelResponse.Data.Data)
                    {
                        string rawUrl = item.CardPhotos?.FirstOrDefault()?.Sizes?.UrlTemplate;

                        string finalImageUrl = "https://via.placeholder.com/800x600?text=No+Photo+Available";

                        if (!string.IsNullOrEmpty(rawUrl))
                        {
                            finalImageUrl = rawUrl.Replace("{width}", "800").Replace("{height}", "600");
                        }

                        resultList.Add(new Hotel
                        {
                            Id = item.Id,
                            Name = item.Title,
                            Price = item.PriceForDisplay ?? "Ціна за запитом",
                            Rating = item.BubbleRating?.Rating?.ToString() ?? "Немає оцінок",

                            Url = $"https://www.tripadvisor.com/Hotel_Review-g{locationId}-d{item.Id}",

                            ImageUrl = finalImageUrl
                        });
                    }
                }
                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка пошуку готелів: {ex.Message}");
                return new List<Hotel>();
            }
        }

        public async Task<List<string>> GetHotelAmenitiesAsync(string hotelId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/getHotelDetails?id={hotelId}&checkIn=2026-06-01&checkOut=2026-06-05&currencyCode=USD"),
                Headers =
                {
                    { "x-rapidapi-key", _apiKey },
                    { "x-rapidapi-host", _apiHost },
                }
            };

            try
            {
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                using var docDetails = JsonDocument.Parse(body);
                var amenitiesList = new List<string>();

                var data = docDetails.RootElement.GetProperty("data");

                if (data.TryGetProperty("amenitiesScreen", out var amenitiesScreen) && amenitiesScreen.ValueKind == JsonValueKind.Array)
                {
                    foreach (var category in amenitiesScreen.EnumerateArray())
                    {
                        if (category.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in contentArray.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    string amenity = item.GetString();
                                    if (!amenitiesList.Contains(amenity))
                                    {
                                        amenitiesList.Add(amenity);
                                    }
                                }
                            }
                        }
                    }
                }

                var topAmenities = amenitiesList.Take(12).ToList();

                if (topAmenities.Count == 0)
                {
                    topAmenities.Add("Детальна інформація відсутня.");
                }

                return topAmenities;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка отримання деталей готелю: {ex.Message}");
                return new List<string> { "Не вдалося завантажити деталі через помилку." };
            }
        }

        public async Task<List<string>> GetHotelReviewsAsync(string hotelId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/getHotelDetails?id={hotelId}&checkIn=2026-06-01&checkOut=2026-06-05&currencyCode=USD"),
                Headers =
                {
                    { "x-rapidapi-key", _apiKey },
                    { "x-rapidapi-host", _apiHost },
                }
            };

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var reviewsList = new List<string>();

            var data = doc.RootElement.GetProperty("data");

            if (data.TryGetProperty("reviews", out var reviewsElement) &&
                reviewsElement.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contentArray.EnumerateArray().Take(5))
                {
                    string title = item.GetProperty("title").GetString();
                    string text = item.GetProperty("text").GetString();

                    if (text.Length > 200) text = text.Substring(0, 200) + "...";

                    reviewsList.Add($"💬 *{title}*\n_{text}_");
                }
            }

            if (reviewsList.Count == 0)
            {
                reviewsList.Add("На жаль, відгуків для цього готелю поки немає.");
            }

            return reviewsList;
        }
    }
}
