using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Opc.UaFx.Client;
using System.Linq;
using Opc.UaFx;
using System.Collections.Generic;

namespace TelegramObcuaBot
{
    internal class BotCommandManager
    {
        public const int POS_OF_COMMAND_PARAMS = 1;
        public const int POS_OF_COMMAND = 0;
        public const int NUMBER_OF_PARAMS_IN_CONNECTION = 3;
        public const char PARAMS_SEPARATOR = '!';

        Message message;
        internal ITelegramBotClient botClient;
        public string[] BotCommands;
        static bool isConnected = false;
        private static OpcClient _client;
        private static bool isTag = false;

        private static string _ip;
        private static string _login;
        private static string _password;

        private static string _node;

        public static Queue<string> alertsQueue = new Queue<string>();

        public int nodeSeverity;

        public string UserData
        {
            set
            {
                var array = value.Split(" ")[POS_OF_COMMAND_PARAMS].Split(PARAMS_SEPARATOR);
                _ip = array.FirstOrDefault();
                _login = array[1];
                _password = array[2];
            }
        }


        /**
         * Конструктор программы
         * 
         * @param message сообщение, отправленное пользователем
         * @param botClient данные бота
         * @param BotCommands список команд бота
         */
        internal BotCommandManager(Message message, ITelegramBotClient botClient, string[] BotCommands)
        {
            this.message = message;
            this.botClient = botClient;
            this.BotCommands = BotCommands;
        }




        /**
         * Менеджер управления командами
         * 
         * @param message сообщение, отправленное пользователем
         */
        internal async Task Manager()
        {
            switch (message.Text)
            {
                case Commands.StartCommand:
                    await StartMessageAsync();

                    return;
                case Commands.HelpCommand:
                    await HelpMessageAsync();

                    return;
                case Commands.DisconnectCommand:
                    await DisconnectMessageAsync();

                    return;
                case Commands.getServerInfoCommand:
                    await GetServerInfoAsync();

                    return;
                case Commands.checkSubscribtionsCommand:
                    await checkSubscriptionsAsync();

                    return;
                default:
                    await CommandWithArgs();

                    return;
                    
            }
        }

        /**
         * Стартовое приветствие
         *
         */
        async Task StartMessageAsync()
        {
            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.GreetingsMessage);
        }

        /**
         * Вызов справки по командам
         *
         */
        async Task HelpMessageAsync()
        {
            var infoList = "Список команд: \n";

            for (int i = 0; i < BotCommands.Length; i++)
            {
                infoList += BotCommands[i] + "\n";
            }
            await botClient.SendTextMessageAsync(message.Chat, infoList);
        }

        /**
         * Проверка ноды
         *
         */
        async Task CheckMessageAsync()
        {
            // запуск метода считывания данных с ноды
            _node = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS];
            await ReadNode();
        }

        /**
         * Выход из аккаунта (удаление данных пользователя)
         *
         */
        async Task DisconnectMessageAsync()
        {
            if (isConnected)
            {
                isConnected = false;
                _login = "";
                _password = "";
                _ip = "";
                _client.Disconnect();

                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.DisconnectedMessage);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotNecessaryDisconnectMessage);
            }
        }

        /**
         * Подключение к серверу
         *
         * @param _login логин пользователя
         * @param _password пароль пользователя
         */
        async Task ConnectToServer()
        {
            _client = new OpcClient(_ip);
            _client.Security.UserIdentity = new OpcClientIdentity(_login, _password);

            try
            {
                _client.Connect();
                isConnected = true;
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.SuccessConnectionMessage);

            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongDataMessage);

                _ip = null;
                _login = null;
                _password = null;
            }

            Console.WriteLine(_client.State.ToString());
        }

        /**
         * Считывание данных с ноды
         *
         * @param _node аргументы ноды 
         */
        async Task ReadNode()
        {
            OpcValue opcValue;
            try
            {
                opcValue = _client.ReadNode(_node);

            } 
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongNodeMessage);
                return;
            }
            if (opcValue.Value != null)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NodeInputMessage + opcValue.Value.ToString());
            } 
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongNodeMessage);
            }

        }

        /**
         * Проверка и подключение к серверу
         *
         * @param _node аргументы ноды 
         */
        async Task ConnectMessageAsync()
        {
            if (message.Text.Split(" ")[POS_OF_COMMAND_PARAMS].Split(PARAMS_SEPARATOR).Length == NUMBER_OF_PARAMS_IN_CONNECTION)
            {
                UserData = message.Text;
                await ConnectToServer();
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongDataMessage);
            }
        }

        async Task GetInfoAsync()
        {
            // запуск метода считывания данных с ноды
            _node = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS];

            OpcNodeInfo opcNodeInfo;

            OpcBrowseNode browse;
            try
            {
                browse = new OpcBrowseNode(nodeId: OpcNodeId.Parse(_node), degree: OpcBrowseNodeDegree.Self, referenceTypes: new[]
                {
                    OpcReferenceType.HasTypeDefinition,
                    OpcReferenceType.Organizes,
                    OpcReferenceType.HasComponent,
                    OpcReferenceType.HasProperty
                });

                browse.Options = OpcBrowseOptions.IncludeAll;
                opcNodeInfo = _client.BrowseNode(browse);
            }
            catch(Exception)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongNodeMessage);
                return;
            }

            if (opcNodeInfo.Name.IsNull)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongNodeMessage);
                return;
            }

            //test
            var referenceNodes = opcNodeInfo.Children();

            var NodeType = referenceNodes.ToArray()[0].Reference.DisplayName.ToString();

            var info =
                    $"Информация о ноде: " +
                    $"\nNodeType:  {NodeType}" +
                    $"\nNodeId: {opcNodeInfo.Attribute(OpcAttribute.NodeId).Value}" +
                    $"\nBrowseName: {opcNodeInfo.Attribute(OpcAttribute.BrowseName).Value}" +
                    $"\nDisplayName: {opcNodeInfo.Attribute(OpcAttribute.DisplayName).Value}" +
                    $"\nDescription: {((opcNodeInfo.Attribute(OpcAttribute.Description).Value.ToString() == "") ? MessageStrings.NoDescriptionMessage : opcNodeInfo.Attribute(OpcAttribute.Description).Value)}";

            //переписать красиво без строк в чистом виде, switch?
            if (NodeType == "TagType")
            {

                await botClient.SendTextMessageAsync(message.Chat, $"{info}" +
                    $"\nValue: {opcNodeInfo.Attribute(OpcAttribute.Value).Value}" +
                    $"\nValueType: {opcNodeInfo.Attribute(OpcAttribute.Value).Value.DataType.ToString()}");
            }
            else if (NodeType == "JobType")
            {
                await botClient.SendTextMessageAsync(message.Chat, info);

                //работает 2 раза без return
                foreach (var referenceNode in referenceNodes)
                {
                    var opcValue = _client.ReadNode(referenceNode.NodeId);
                    Console.WriteLine(referenceNode.Reference.DisplayName + " " + opcValue);

                    if (referenceNode.Reference.DisplayName == "Status")
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Статус работы: " + ((JobStatus)int.Parse(opcValue.ToString())).ToString());

                        return;
                    }
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, info);
            }

        }

        async Task SetValueAsync()
        {
            if (message.Text.Split(" ")[POS_OF_COMMAND_PARAMS].Split(PARAMS_SEPARATOR).Length != 2)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongDataMessage);

                return;
            }

            _node = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS].Split(PARAMS_SEPARATOR)[0];
            var changedValue = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS].Split(PARAMS_SEPARATOR)[1];

            OpcValue tempValue;
            try
            {
                await IsTagAsync();

                tempValue = _client.ReadNode(_node);
                Console.WriteLine(_client.WriteNode(_node, changedValue));

                if (!isTag)
                {
                    await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NodeIsNotTagMessage);

                    return;
                }

            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongNodeMessage);

                return;
            }

            //возможно нужны доп проверки для некоторых типов
            if (_client.ReadNode(_node).Value == null)
            {
                _client.WriteNode(_node, tempValue.Value);

                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongTypeOfInputMessage);
                return;
            }

            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.InputValueSetMessage);
        }

        async Task GetServerInfoAsync()
        {
            if (!isConnected)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }

            await botClient.SendTextMessageAsync(message.Chat, "Информация о сервере: " +
                $"\nServerUri: {_client.ServerAddress}" +
                $"\nHostName: {_client.ServerAddress.Host}" +
                $"\nClientState: {_client.State}" +
                $"\nNamespaces: {_client.Namespaces.Count}" +
                $"\nKeepAlive: {_client.KeepAlive}");
        }

        enum JobStatus
        {
            Started = 3,
            Stopped = 0,
            Error = 4
        }

        async Task IsTagAsync()
        {
            var browse = new OpcBrowseNode(nodeId: _node, degree: OpcBrowseNodeDegree.Self, referenceTypes: new[]
                {
                OpcReferenceType.HasTypeDefinition
            });

            browse.Options = OpcBrowseOptions.IncludeAll;
            var opcNodeInfo = _client.BrowseNode(browse);

            var NodeType = opcNodeInfo.Children().ToArray()[0].Reference.DisplayName.ToString();

            if (NodeType != "TagType")
            {
                isTag = false;
                return;
            }

            isTag = true;
        }

        async Task subscribeOnAlarmAsync()
        {
            if (!isConnected)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotConnectedMessage);
                return;
            }
            var severityStr = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS];

            try
            {
                nodeSeverity = int.Parse(severityStr);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.IncorrectDataTypeMessage);
                return;
            }

            var currentAlarmSeverity = new OpcSimpleAttributeOperand(OpcEventTypes.Event, "Severity");
            var filter = OpcFilter.Using(_client)
                .FromEvents(OpcEventTypes.AlarmCondition,
                            OpcEventTypes.ExclusiveLimitAlarm,
                            OpcEventTypes.DialogCondition)
                .Where(currentAlarmSeverity >= nodeSeverity)
                .Select();
            _client.SubscribeEvent(
                OpcObjectTypes.Server,
                filter,
                HandleGlobalEvents);
            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.SuccecsfullySubscribedMessage);
        }

        private void HandleGlobalEvents(object sender, OpcEventReceivedEventArgs e)
        {
            alertsQueue.Enqueue(
                $"Источник {e.Event.SourceName}" +
                $"\nId ноды: {e.Event.SourceNodeId}" +
                $"\nСообщение: {e.Event.Message}" +
                $"\nSeverity: {e.Event.Severity}" +
                $"\nВремя получения: {e.Event.ReceiveTime}");

            sendAlertAsync();
        }

        async Task sendAlertAsync()
        {
            while (alertsQueue.Count > 0)
            {
                var alert = alertsQueue.Dequeue();
                await botClient.SendTextMessageAsync(message.Chat, $"Обнаружен аларм тяжестью равного {nodeSeverity} или выше: \n{alert}");
            }
        }

        async Task checkSubscriptionsAsync()
        {
            if (!isConnected)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }

            var list = "";
            for (int i = 0; i < _client.Subscriptions.Count; i++)
            {
                OpcEventFilter opcEventFilter = (OpcEventFilter)_client.Subscriptions[i].MonitoredItems[0].Filter;
                var sev = opcEventFilter.WhereClause.Elements[0].Operands[1];
                list += 
                    $"Id подписки: {_client.Subscriptions[i].Id} " +
                    $"Severity: {sev} \n " +
                    $"Информация: {_client.Subscriptions[i]}\n";
            }
            
            await botClient.SendTextMessageAsync(message.Chat, "Список подписок на алармы: \n" + list);
        }

        async Task CommandWithArgs()
        {
            if (message.Text.Split(" ").Length == 2)
            {
                switch (message.Text.Split(" ")[POS_OF_COMMAND])
                {
                    case Commands.GetValueCommand:
                        await CheckMessageAsync();

                        return;
                    case Commands.setValueCommand:
                        await SetValueAsync();

                        return;
                    case Commands.ConnectCommand:
                        if (!isConnected)
                        {
                            await ConnectMessageAsync();
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotNecessaryConnectMessage);
                        }

                        return;
                    case Commands.getInfoCommand:
                        await GetInfoAsync();

                        return;
                    case Commands.subscribeOnAlarmCommand:
                        await subscribeOnAlarmAsync();

                        return;
                    case Commands.unsubscribeCommand:
                        await unsubscribeAsync();

                        return;
                }
            }

            if (message.Text.Split(" ")[POS_OF_COMMAND] == Commands.unsubscribeCommand
                || message.Text.Split(" ")[POS_OF_COMMAND] == Commands.subscribeOnAlarmCommand
                || message.Text.Split(" ")[POS_OF_COMMAND] == Commands.getInfoCommand
                || message.Text.Split(" ")[POS_OF_COMMAND] == Commands.setValueCommand
                || message.Text.Split(" ")[POS_OF_COMMAND] == Commands.GetValueCommand)
            {
                if (!isConnected)
                {
                    await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotConnectedMessage);

                    return;
                }

                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongDataMessage);

                return;
            }
            else if (message.Text.Split(" ")[POS_OF_COMMAND] == Commands.ConnectCommand)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongDataMessage);

                return;
            }

            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.WrongCommandMessage);

            return;
        }

        async Task unsubscribeAsync()
        {
            var subId = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS];

            var subscribeId = -1;
            try
            {
                subscribeId = int.Parse(subId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.IncorrectDataTypeMessage);
                return;
            }

            for (int i = 0; i < _client.Subscriptions.Count; i++)
            {
                if(subscribeId == _client.Subscriptions[i].Id)
                {
                    _client.Subscriptions[i].Unsubscribe();
                    await botClient.SendTextMessageAsync(message.Chat, MessageStrings.SuccecsfullyUnsubscribedMessage);

                    return;
                }

            }

            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.CannotFindIdMessage);

        }


    }
}
