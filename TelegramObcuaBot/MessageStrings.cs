using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramOpcuaBot
{
    /// <summary>
    /// Класс текстовых полей сообщений бота
    /// </summary>
    internal class MessageStrings
    {
        public static string GreetingsMessage = "Приветствую, введите /help для для того чтобы посмотреть на мои возможности.";
        public static string WrongCommandMessage = "Ничего не понял, повторите ввод или введите /help для для того чтобы посмотреть на мои возможности.";
        public static string WrongDataMessage = "Введены неверные данные!";
        public static string NotConnectedMessage = "Сперва нужно подключится к серверу!";
        public static string DisconnectedMessage = "Вы отключились от сервера.";
        public static string NotNecessaryDisconnectMessage = "Вы и так не подключены к серверу.";
        public static string NotNecessaryConnectMessage = "Вы и так подключены к серверу.";
        public static string SuccessConnectionMessage = "Подключение успешно!";
        public static string WrongNodeMessage = "Неверно введена нода!";
        public static string NodeInputMessage = "Значение ноды: ";
        public static string NodeIsNotTagMessage = "Нода не является тэгом!";
        public static string WrongTypeOfInputMessage = "Тип введенной переменной не соответствует типу ноды!";
        public static string InputValueSetMessage = "Значение установлено!";
        public static string NoDescriptionMessage = "Нет описания.";
        public static string IncorrectDataTypeMessage = "Неверный тип введенных данных!";
        public static string SuccecsfullyUnsubscribedMessage = "Вы успешно отписались от получения уведомлений выбранной подписки.";
        public static string SuccecsfullySubscribedMessage = "Подписка успешно подключена.";
        public static string CannotFindIdMessage = "Id подписки не найден!";

        public static string[] BotCommands =
        {
            Commands.StartCommand + " - запуск бота",
            Commands.HelpCommand + " - справка по командам",
            Commands.ConnectCommand + " ip!login!password - подключится к серверу",
            Commands.GetValueCommand + " NodeId - считать показатели",
            Commands.SetValueCommand + " NodeId!newValue - установить значение",
            Commands.GetInfoCommand + " NodeId - информация о ноде",
            Commands.GetServerInfoCommand + " доступная информация о сервере",
            Commands.DisconnectCommand + " - отключится от сервера",
            Commands.SubscribeOnAlarmCommand+ " Severity - подписатся на алярм с указанной тяжестью",
            Commands.CheckSubscribtionsCommand + " - проверить свои подписки",
            Commands.UnsubscribeCommand + " subscribeId - отписаться от получения уведомлений об алармах"
        };
    }

    /// <summary>
    /// Класс команд на которые реагирует телеграм бот
    /// </summary>
    internal class Commands
    {
        public const string ConnectCommand = "/connect";
        public const string StartCommand = "/start";
        public const string HelpCommand = "/help";
        public const string GetValueCommand = "/getValue";
        public const string DisconnectCommand = "/disconnect";
        public const string SetValueCommand = "/setValue";
        public const string GetInfoCommand = "/getInfo";
        public const string GetServerInfoCommand = "/getServerInfo";
        public const string SubscribeOnAlarmCommand = "/subscribeOnAlarm";
        public const string CheckSubscribtionsCommand = "/checkSubscribtions";
        public const string UnsubscribeCommand = "/unsubscribe";

    }
}
