using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;

namespace TelegramObcuaBot
{
    internal class BotCommandManager
    {
        Message message;
        internal ITelegramBotClient botClient;
        public string[] BotCommands;
        static bool isTryingLoggedIn = false;
        static bool isLoggedIn = false;
        private string[] _loginData;

        internal BotCommandManager(Message message, ITelegramBotClient botClient, string[] BotCommands, String[] _loginData)
        {
            this.message = message;
            this.botClient = botClient;
            this.BotCommands = BotCommands;
            this._loginData = _loginData;
        }
        internal async Task Manager()
        {
            switch(message.Text)
            {
                case "/start":
                    await StartMessageAsync();

                    return;
                case "/help":
                    await HelpMessageAsync();

                    return;
                case "/login":
                    await LoginMessageAsync();

                    return;
                case "/check":
                    await CheckMessageAsync();

                    return;
                case "/logout":
                    await LogoutMessageAsync();

                    return;
                default:
                    if (isTryingLoggedIn)
                    {
                        await EnterLoginData();

                        return;
                    }
                    await botClient.SendTextMessageAsync(message.Chat, "Ничего не понял, повтори ввод или введи /help для для того чтобы посмотреть на мои возможности! ");

                    return;
            }
        }
        async Task StartMessageAsync()
        {
            await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать на борт, введи /help для для того чтобы посмотреть на мои возможности!");
        }

        async Task HelpMessageAsync()
        {
            var info = "Список команд: \n";

            for (int i = 0; i < BotCommands.Length; i++)
            {
                info += BotCommands[i] + "\n";
            }
            await botClient.SendTextMessageAsync(message.Chat, info);
        }

        async Task LoginMessageAsync()
        {
            await botClient.SendTextMessageAsync(message.Chat, "Введите через пробел IP сервера, свой логин и пароль: ");
            //login-code
            isTryingLoggedIn = true;
        }

        async Task CheckMessageAsync()
        {
            if (!isLoggedIn)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Сперва нужно залогинится!");
                return;
            }
            //code
            await botClient.SendTextMessageAsync(message.Chat, "data (заглушка)");
        }

        async Task LogoutMessageAsync()
        {
            if (isLoggedIn)
            {
                isLoggedIn = false;
                await botClient.SendTextMessageAsync(message.Chat, "Вы вышли из аккаунта!");
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, "Вы не залогинены, вам не нужно выходить из аккаунта!");
            }
        }

        async Task EnterLoginData()
        {
            Console.WriteLine(message.Text);

            _loginData = message.Text.Split(" ");
            //вывод информации в консоль
            Console.WriteLine("Данные для входа в аккаунт: ");
            for (int i = 0; i < _loginData.Length; i++)
            {
                Console.WriteLine(_loginData[i]);
            }

            //проверка на правильность введенных данных (доделать)
            if (_loginData.Length == 3)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Вы залогинились!");
                isLoggedIn = true;
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, "Введены неверные данные!");
            }

            isTryingLoggedIn = false;
        }

    }
}
