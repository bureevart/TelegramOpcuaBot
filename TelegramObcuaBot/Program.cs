using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using System.Configuration;

namespace TelegramOpcuaBot
{

    /// <summary>
    /// Класс телеграм бота
    /// </summary>
    public class TelegramOpcuaBot
    {
        private static string _token = ConfigurationManager.AppSettings["token"];
        static BotCommandManager _botCommandManager;

        private static ITelegramBotClient _bot;

        /// <summary>
        /// Обработчик обновлений бота, считывает сообщения обрабатывает их
        /// </summary>
        /// <param name="botClient">Телеграм бот</param>
        /// <param name="update">Обновление</param>
        /// <param name="cancellationToken">Уведомление о том что операция должна быть отменена</param>
        /// <returns>Task (вызывающий код будет ждать завершение метода)</returns>
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                _botCommandManager = new BotCommandManager(message, botClient);
                await _botCommandManager.Manager();
                
            }

        }

        /// <summary>
        /// Обработчик исключение (выводит информацию об ошибки в консоль)
        /// </summary>
        /// <param name="botClient">телеграм бот</param>
        /// <param name="exception">исключение</param>
        /// <param name="cancellationToken">Уведомление о том что операция должна быть отменена</param>
        /// <returns>Task (вызывающий код будет ждать завершение метода)</returns>
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        /// <summary>
        /// Основной метод программы
        /// </summary>
        static void Main()
        {
            if(_token == null)
            {
                Console.WriteLine("Введен неверный токен!");
                return;
            }

            _bot = new TelegramBotClient(_token);
            Console.WriteLine("Запущен бот " + _bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.ReadLine();
        }
    }
}