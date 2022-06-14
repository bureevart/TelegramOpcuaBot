using System;
using System.Collections.Generic;
using System.Text;

namespace TelegramObcuaBot
{
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
            Commands.setValueCommand + " NodeId!newValue - установить значение",
            Commands.getInfoCommand + " NodeId - информация о ноде",
            Commands.getServerInfoCommand + " доступная информация о сервере",
            Commands.DisconnectCommand + " - отключится от сервера",
            Commands.subscribeOnAlarmCommand+ " Severity - подписатся на алярм с указанной тяжестью",
            Commands.checkSubscribtionsCommand + " - проверить свои подписки",
            Commands.unsubscribeCommand + " subscribeId - отписаться от получения уведомлений об алармах"
        };
    }

    internal class Commands
    {
        public const string ConnectCommand = "/connect";
        public const string StartCommand = "/start";
        public const string HelpCommand = "/help";
        public const string GetValueCommand = "/getValue";
        public const string DisconnectCommand = "/disconnect";
        public const string setValueCommand = "/setValue";
        public const string getInfoCommand = "/getInfo";
        public const string getServerInfoCommand = "/getServerInfo";
        public const string subscribeOnAlarmCommand = "/subscribeOnAlarm";
        public const string checkSubscribtionsCommand = "/checkSubscribtions";
        public const string unsubscribeCommand = "/unsubscribe";

    }
}
