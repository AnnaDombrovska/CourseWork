using HotelTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq; 

namespace HotelTelegramBot.Handlers
{
    internal class MessageHandler
    {
        private readonly IHotelApiService _hotelService;

        public MessageHandler(IHotelApiService hotelService)
        {
            _hotelService = hotelService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message) return;
            if (message.Text is not { } messageText) return;

            long chatId = message.Chat.Id;
            Console.WriteLine($"[LOG] Користувач написав: '{messageText}'");

            if (messageText.ToLower() == "/start")
            {
                var replyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "🔍 Новий пошук", "💰 Пошук за ціною" },
                    new KeyboardButton[] { "❤️ Моє Обране" }
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Привіт! Я твій особистий помічник з пошуку готелів 🏨\n\nПросто напиши мені назву міста англійською (наприклад, Kyiv чи Lviv), і я знайду для тебе найкращі варіанти!\n\nТакож ти можеш скористатися кнопками меню нижче для пошуку під конкретний бюджет або для перегляду своїх збережених готелів.",
                    replyMarkup: replyKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            if (messageText == "🔍 Новий пошук")
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Напиши мені назву міста англійською (наприклад, Kyiv, Lviv чи Warsaw), і я знайду всі доступні варіанти! 🌍",
                    cancellationToken: cancellationToken);
                return;
            }

            if (messageText == "💰 Пошук за ціною")
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "💰 ПОШУК ЗА БЮДЖЕТОМ\n\nОскільки міжнародні готелі фіксують ціни у валюті, будь ласка, вказуй свій ліміт у **доларах ($)**.\n\nПросто введи **назву міста** англійською та **максимальну ціну** через кому.\n\nПриклади:\n`Lviv, 100`\n`Warsaw, 150`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            if (messageText == "❤️ Моє Обране")
            {
                using var db = new ApplicationDbContext();
                var favorites = db.FavoriteHotels.Where(f => f.ChatId == chatId).ToList();

                if (!favorites.Any())
                {
                    await botClient.SendMessage(chatId, "Твій список обраного поки що порожній. 📭", cancellationToken: cancellationToken);
                    return;
                }

                await botClient.SendMessage(chatId, "✨ Твої збережені готелі:", cancellationToken: cancellationToken);

                foreach (var fav in favorites)
                {
                    string favText = $"🏨 *{fav.Name}*\n💰 Ціна: {fav.Price}\n⭐ Рейтинг: {fav.Rating}";

                    var deleteKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("❌ Видалити з обраного", $"fav_rem_{fav.Id}"),
                            InlineKeyboardButton.WithUrl("🌐 На сайті", fav.Url)
                        }
                    });

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: favText,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: deleteKeyboard,
                        cancellationToken: cancellationToken);
                }
                return;
            }

            if (messageText.Contains(","))
            {
                var parts = messageText.Split(',');
                if (parts.Length == 2)
                {
                    string cityName = parts[0].Trim();
                    string priceInput = parts[1].Trim();

                    string cleanUserPrice = new string(priceInput.Where(char.IsDigit).ToArray());

                    if (int.TryParse(cleanUserPrice, out int maxPrice))
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: $"Шукаю готелі у місті {cityName} з ціною до ${maxPrice}... ⏳",
                            cancellationToken: cancellationToken);

                        var locId = await _hotelService.GetLocationIdAsync(cityName);

                        if (string.IsNullOrEmpty(locId))
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: $"На жаль, місто '{cityName}' не знайдено. 😔 Перевірте правильність написання англійською.",
                                cancellationToken: cancellationToken);
                            return;
                        }

                        var allHotels = await _hotelService.GetHotelsAsync(locId);

                        var filteredHotels = allHotels.Where(hotel =>
                        {
                            string cleanHotelPrice = new string(hotel.Price.Where(char.IsDigit).ToArray());
                            if (int.TryParse(cleanHotelPrice, out int hotelPrice))
                            {
                                return hotelPrice <= maxPrice;
                            }
                            return false;
                        }).Take(10).ToList();

                        if (!filteredHotels.Any())
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: $"За вашим бюджетом у ${maxPrice} нічого не знайдено. 😔 Спробуйте ввести більшу суму.",
                                cancellationToken: cancellationToken);
                            return;
                        }

                        await botClient.SendMessage(chatId, $"Ось що підходить під ліміт ${maxPrice} (Топ-10 варіантів):", cancellationToken: cancellationToken);

                        foreach (var hotel in filteredHotels)
                        {
                            string text = $"🏨 *{hotel.Name}*\n💰 Ціна: {hotel.Price}\n⭐ Рейтинг: {hotel.Rating}";

                            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("ℹ️ Детальніше", $"details_{hotel.Id}"),
                                    InlineKeyboardButton.WithCallbackData("💬 Відгуки", $"reviews_{hotel.Id}")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("❤️ В обране", $"fav_add_{hotel.Id}"),
                                    InlineKeyboardButton.WithUrl("🌐 На сайті", hotel.Url)
                                }
                            });

                            await botClient.SendPhoto(
                              chatId: chatId,
                              photo: InputFile.FromUri(hotel.ImageUrl),
                              caption: text,
                              parseMode: ParseMode.Markdown,
                              replyMarkup: inlineKeyboard,
                              cancellationToken: cancellationToken);
                        }
                        return; 
                    }
                }
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Шукаю найкращі готелі у місті {messageText}... ⏳",
                cancellationToken: cancellationToken);

            var regularLocId = await _hotelService.GetLocationIdAsync(messageText);

            if (string.IsNullOrEmpty(regularLocId))
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"На жаль, я не зміг знайти місто '{messageText}'. 😔 Перевірте правильність написання англійською (наприклад: Lviv, Kyiv).",
                    cancellationToken: cancellationToken);
                return;
            }

            var allRegularHotels = await _hotelService.GetHotelsAsync(regularLocId);
            var hotelsList = allRegularHotels.Take(10).ToList();

            await botClient.SendMessage(chatId, "Ось топ-10 варіантів для вас:", cancellationToken: cancellationToken);

            foreach (var hotel in hotelsList)
            {
                string text = $"🏨 *{hotel.Name}*\n💰 Ціна: {hotel.Price}\n⭐ Рейтинг: {hotel.Rating}";

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("ℹ️ Детальніше", $"details_{hotel.Id}"),
                        InlineKeyboardButton.WithCallbackData("💬 Відгуки", $"reviews_{hotel.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("❤️ В обране", $"fav_add_{hotel.Id}"),
                        InlineKeyboardButton.WithUrl("🌐 На сайті", hotel.Url)
                    }
                });

                await botClient.SendPhoto(
                  chatId: chatId,
                  photo: InputFile.FromUri(hotel.ImageUrl),
                  caption: text,
                  parseMode: ParseMode.Markdown,
                  replyMarkup: inlineKeyboard,
                  cancellationToken: cancellationToken);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine($"\n[ПОМИЛКА]: {errorMessage}");
            return Task.CompletedTask;
        }
    }
}