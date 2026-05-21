using HotelTelegramBot.Handlers;
using HotelTelegramBot.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace HotelTelegramBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();

            // ТУТ ЗМІНЕНО: Замість MockHotelService підключаємо реальний TripadvisorService
            IHotelApiService baseService = new TripadvisorService(config);

            // 2. Обертаємо її в нашого "охоронця" з кешем
            // Це ідеальне рішення для захисту курсової! Наш CachedHotelService буде зберігати 
            // результати у пам'яті, і якщо користувач повторно введе те саме місто, 
            // бот не буде витрачати обмежені запити (наші 50 штук на місяць) на RapidAPI.
            IHotelApiService hotelService = new CachedHotelService(baseService);

            var messageHandler = new MessageHandler(hotelService);
            var callbackHandler = new CallbackHandler(hotelService);

            string botToken = config["TelegramBot:Token"];
            var botClient = new TelegramBotClient(botToken);
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            botClient.StartReceiving(
                updateHandler: async (bot, update, ct) =>
                {
                    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                    {
                        await messageHandler.HandleUpdateAsync(bot, update, ct);
                    }
                    else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                    {
                        await callbackHandler.HandleCallbackQueryAsync(bot, update.CallbackQuery, ct);
                    }
                },
                errorHandler: (bot, ex, source, ct) => messageHandler.HandlePollingErrorAsync(bot, ex, ct),
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} запущений");
            Console.WriteLine("Натисніть Enter у консолі, щоб вимкнути сервер");

            Console.ReadLine();
            cts.Cancel();
        }
    }
}