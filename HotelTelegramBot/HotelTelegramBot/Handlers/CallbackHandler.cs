using HotelTelegramBot.Models;
using HotelTelegramBot.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HotelTelegramBot.Handlers
{
    internal class CallbackHandler
    {
        private readonly IHotelApiService _hotelService;

        public CallbackHandler(IHotelApiService hotelService)
        {
            _hotelService = hotelService;
        }

        public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Data is not { } callbackData) return;
            if (callbackQuery.Message is not { } message) return;

            long chatId = message.Chat.Id;

            if (callbackData.StartsWith("details_"))
            {
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Завантажую деталі...",
                    cancellationToken: cancellationToken);

                string hotelId = callbackData.Substring(8);

                string messageContent = message.Text ?? message.Caption;

                if (string.IsNullOrEmpty(messageContent)) return; 

                string hotelName = messageContent.Split('\n')[0]
                                     .Replace("🏨", "")
                                     .Replace("*", "")
                                     .Trim();

                var amenities = await _hotelService.GetHotelAmenitiesAsync(hotelId);
                string amenitiesText = string.Join("\n✅ ", amenities);
                string responseText = $"🏨 *{hotelName}*\n\n*Зручності у готелі:*\n✅ {amenitiesText}";

                await botClient.SendMessage(
                    chatId: chatId,
                    text: responseText,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            if (callbackData.StartsWith("reviews_"))
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Завантажую відгуки...", cancellationToken: cancellationToken);

                string hotelId = callbackData.Replace("reviews_", "");

                string messageContent = callbackQuery.Message.Text ?? callbackQuery.Message.Caption;
                string hotelName = string.IsNullOrEmpty(messageContent) ? "Готелю" : messageContent.Split('\n')[0].Replace("🏨", "").Replace("*", "").Trim();

                var reviews = await _hotelService.GetHotelReviewsAsync(hotelId);

                string reviewsText = $"💬 *Останні відгуки для {hotelName}:*\n\n" + string.Join("\n\n", reviews);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: reviewsText,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            if (callbackData.StartsWith("fav_add_"))
            {
                string hotelId = callbackData.Replace("fav_add_", "");

                using var db = new ApplicationDbContext();

                bool exists = db.FavoriteHotels.Any(f => f.ChatId == chatId && f.HotelId == hotelId);
                if (exists)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "Цей готель вже є у твоєму обраному! 😉", cancellationToken: cancellationToken);
                    return;
                }

                string messageContent = message.Text ?? message.Caption;

                if (string.IsNullOrEmpty(messageContent)) return; 

                string[] lines = messageContent.Split('\n');
                string hotelName = lines[0].Replace("🏨", "").Replace("*", "").Trim();

                string price = lines.Length > 1 ? lines[1].Replace("💰 Ціна:", "").Trim() : "Невідомо";
                string rating = lines.Length > 2 ? lines[2].Replace("⭐ Рейтинг:", "").Trim() : "Невідомо";

                var newFavorite = new FavoriteHotel
                {
                    ChatId = chatId,
                    HotelId = hotelId,
                    Name = hotelName,
                    Price = price,
                    Rating = rating,
                    Url = "https://www.tripadvisor.com" 
                };

                db.FavoriteHotels.Add(newFavorite);
                await db.SaveChangesAsync(cancellationToken);

                await botClient.AnswerCallbackQuery(callbackQuery.Id, "❤️ Додано в обране!", cancellationToken: cancellationToken);
                return;
            }

            if (callbackData.StartsWith("fav_rem_"))
            {
                if (int.TryParse(callbackData.Replace("fav_rem_", ""), out int dbId))
                {
                    using var db = new ApplicationDbContext();
                    var hotelToRemove = db.FavoriteHotels.FirstOrDefault(f => f.Id == dbId);

                    if (hotelToRemove != null)
                    {
                        db.FavoriteHotels.Remove(hotelToRemove);
                        await db.SaveChangesAsync(cancellationToken);

                        await botClient.AnswerCallbackQuery(callbackQuery.Id, "💥 Видалено з обраного", cancellationToken: cancellationToken);

                        await botClient.DeleteMessage(chatId, message.MessageId, cancellationToken);
                    }
                }
                return;
            }
        }
    }
}