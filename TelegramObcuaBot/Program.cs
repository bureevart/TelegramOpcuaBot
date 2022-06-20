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
    /// Class of telegram bot
    /// </summary>
    public class TelegramOpcuaBot
    {
        private static string _token = ConfigurationManager.AppSettings["token"];
        static BotCommandManager _botCommandManager;

        private static ITelegramBotClient _bot;

        /// <summary>
        /// Bot update handler, reads messages and processes them
        /// </summary>
        /// <param name="botClient">telegram bot</param>
        /// <param name="update">update</param>
        /// <param name="cancellationToken">Notification that the operation should be canceled</param>
        /// <returns>Task (called code will wait end of method)</returns>
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
        /// Error's handler (sended info about exception in consol)
        /// </summary>
        /// <param name="botClient">telegram bot</param>
        /// <param name="exception">exception</param>
        /// <param name="cancellationToken">Notification that the operation should be canceled</param>
        /// <returns>Task (called code will wait end of method)</returns>
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        /// <summary>
        /// Main method of program
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