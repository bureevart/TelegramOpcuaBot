using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Opc.UaFx.Client;
using System.Linq;
using Opc.UaFx;
using System.Collections.Generic;

namespace TelegramOpcuaBot
{
    /// <summary>
    /// Class of bot command manager
    /// </summary>
    /// 
    internal class BotCommandManager
    {
        private int curChatIndex = -1;
        private static List<User> _userList = new List<User>();
        internal ITelegramBotClient botClient;

        /// <summary>
        /// Сonstructor of bot command manager
        /// </summary>
        /// <param name="message">message that sended by user</param>
        /// <param name="botClient">bot</param>
        internal BotCommandManager(Message message, ITelegramBotClient botClient)
        {
            defineChat(message);

            _userList[curChatIndex].AlarmSubscriptions = new AlarmSubscriptions(message, botClient, _userList[curChatIndex].Client, _userList[curChatIndex].isConnected);
            _userList[curChatIndex].message = message;
            this.botClient = botClient;
        }

        /// <summary>
        /// Determining the chat id
        /// </summary>
        /// <param name="message">sended message</param>
        void defineChat(Message message)
        {
            curChatIndex = -1;

            for (int i = 0; i < _userList.Count; i++)
            {
                if (_userList[i].id == message.Chat.Id)
                {
                    curChatIndex = i;

                    break;
                }
            }

            if (curChatIndex == -1)
            {
                _userList.Add(new User());
                curChatIndex = _userList.Count - 1;
                _userList[curChatIndex].id = message.Chat.Id;
            }
        }

        /// <summary>
        /// The bot's command handler, processes the message that has already arrived
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        internal async Task Manager()
        {
            switch (_userList[curChatIndex].message.Text)
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
                case Commands.GetServerInfoCommand:
                    await GetServerInfoAsync();

                    return;
                case Commands.CheckSubscribtionsCommand:
                    await _userList[curChatIndex].AlarmSubscriptions.checkSubscriptionsAsync();

                    return;
                default:
                    await CommandWithArgs();

                    return;
            }
        }
        /// <summary>
        /// Output greetings to the user
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task StartMessageAsync()
        {
            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.GreetingsMessage);
        }
        /// <summary>
        /// Output of bot command list
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task HelpMessageAsync()
        {
            var infoList = "Список команд: \n";

            for (int i = 0; i < MessageStrings.BotCommands.Length; i++)
            {
                infoList += MessageStrings.BotCommands[i] + "\n";
            }
            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, infoList);
        }

        /// <summary>
        /// Output of the value of the node specified in the arguments
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task GetValueAsync()
        {
            if (!_userList[curChatIndex].isConnected)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }

            _userList[curChatIndex].Node = _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS];

            OpcValue opcValue;
            try
            {
                opcValue = _userList[curChatIndex].Client.ReadNode(_userList[curChatIndex].Node);
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongNodeMessage);

                return;
            }

            if (opcValue.Value != null)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NodeInputMessage + opcValue.Value.ToString());
                
                return;
            }

            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongNodeMessage);
        }

        /// <summary>
        /// Closes the active session and stops connecting to the server
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task DisconnectMessageAsync()
        {
            if (_userList[curChatIndex].isConnected)
            {
                _userList[curChatIndex].isConnected = false;
                _userList[curChatIndex].Ip = "";
                _userList[curChatIndex].Login = "";
                _userList[curChatIndex].Password = "";

                _userList[curChatIndex].Client.Disconnect();

                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.DisconnectedMessage);
            }
            else
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotNecessaryDisconnectMessage);
            }
        }

        /// <summary>
        /// Connecting to the server with login and password
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task ConnectToServer()
        {

            _userList[curChatIndex].Client = new OpcClient(_userList[curChatIndex].Ip);
            _userList[curChatIndex].Client.Security.UserIdentity = new OpcClientIdentity(_userList[curChatIndex].Login, _userList[curChatIndex].Password);

            try
            {
                _userList[curChatIndex].Client.Connect();
                _userList[curChatIndex].isConnected = true;
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.SuccessConnectionMessage);

            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongDataMessage);

                _userList[curChatIndex].Ip = null;
                _userList[curChatIndex].Login = null;
                _userList[curChatIndex].Password = null;
            }
            //state handler
            _userList[curChatIndex].Client.StateChanged += Client_StateChanged;
        }

        /// <summary>
        /// state of connecting handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">state changed event</param>
        private async void Client_StateChanged(object sender, OpcClientStateChangedEventArgs e)
        {
            Console.WriteLine(e.NewState);
            switch (e.NewState)
            {
                case OpcClientState.Reconnecting:
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.ReconnectingMessage);
                    break;
                case OpcClientState.Connected:
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.ConnectionRestoredMessage);
                    break;
                //other events
            }
        }

        /// <summary>
        /// Checking parameters before connecting to the server
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task ConnectMessageAsync()
        {
            if (_userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS].Split(User.PARAMS_SEPARATOR).Length == User.NUMBER_OF_PARAMS_IN_CONNECTION)
            {
                _userList[curChatIndex].UserData = _userList[curChatIndex].message.Text;
                await ConnectToServer();
            }
            else
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongDataMessage);
            }
        }

        /// <summary>
        /// Output info about node
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task GetInfoAsync()
        {
            if (!_userList[curChatIndex].isConnected)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }
            _userList[curChatIndex].Node = _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS];

            OpcNodeInfo opcNodeInfo;

            OpcBrowseNode browse;
            try
            {
                browse = new OpcBrowseNode(nodeId: OpcNodeId.Parse(_userList[curChatIndex].Node), degree: OpcBrowseNodeDegree.Self, referenceTypes: new[]
                {
                    OpcReferenceType.HasTypeDefinition,
                    OpcReferenceType.Organizes,
                    OpcReferenceType.HasComponent,
                    OpcReferenceType.HasProperty
                });

                browse.Options = OpcBrowseOptions.IncludeAll;
                opcNodeInfo = _userList[curChatIndex].Client.BrowseNode(browse);
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongNodeMessage);

                return;
            }

            if (opcNodeInfo.Name.IsNull)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongNodeMessage);

                return;
            }

            var referenceNodes = opcNodeInfo.Children();

            var NodeType = referenceNodes.ToArray()[0].Reference.DisplayName.ToString();

            var info =
                    $"Информация о ноде: " +
                    $"\nNodeType:  {NodeType}" +
                    $"\nNodeId: {opcNodeInfo.Attribute(OpcAttribute.NodeId).Value}" +
                    $"\nBrowseName: {opcNodeInfo.Attribute(OpcAttribute.BrowseName).Value}" +
                    $"\nDisplayName: {opcNodeInfo.Attribute(OpcAttribute.DisplayName).Value}" +
                    $"\nDescription: {((opcNodeInfo.Attribute(OpcAttribute.Description).Value.ToString() == "") ? MessageStrings.NoDescriptionMessage : opcNodeInfo.Attribute(OpcAttribute.Description).Value)}";

            switch (NodeType)
            {
                case "TagType":
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, $"{info}" +
                    $"\nValue: {opcNodeInfo.Attribute(OpcAttribute.Value).Value}" +
                    $"\nValueType: {opcNodeInfo.Attribute(OpcAttribute.Value).Value.DataType.ToString()}");

                    return;
                case "JobType":
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, info);

                    foreach (var referenceNode in referenceNodes)
                    {
                        var opcValue = _userList[curChatIndex].Client.ReadNode(referenceNode.NodeId);
                        Console.WriteLine(referenceNode.Reference.DisplayName + " " + opcValue);

                        if (referenceNode.Reference.DisplayName == "Status")
                        {
                            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, "Статус работы: " + ((JobStatus)int.Parse(opcValue.ToString())).ToString());

                            return;
                        }
                    }

                    return;
                default:
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, info);

                    return;
            }
 
        }

        /// <summary>
        /// Set value in selected node
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task SetValueAsync()
        {
            if (!_userList[curChatIndex].isConnected)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }

            if (_userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS].Split(User.PARAMS_SEPARATOR).Length != 2)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongDataMessage);

                return;
            }

            _userList[curChatIndex].Node = _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS].Split(User.PARAMS_SEPARATOR)[0];
            var changedValue = _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND_PARAMS].Split(User.PARAMS_SEPARATOR)[1];

            try
            {
                if (!IsTag())
                {
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NodeIsNotTagMessage);

                    return;
                }
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongNodeMessage);

                return;
            }

            var tempValue = _userList[curChatIndex].Client.ReadNode(_userList[curChatIndex].Node);
            Console.WriteLine(_userList[curChatIndex].Client.WriteNode(_userList[curChatIndex].Node, changedValue));

            if (_userList[curChatIndex].Client.ReadNode(_userList[curChatIndex].Node).Value == null)
            {
                _userList[curChatIndex].Client.WriteNode(_userList[curChatIndex].Node, tempValue.Value);

                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongTypeOfInputMessage);
                return;
            }

            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.InputValueSetMessage);
        }

        /// <summary>
        /// Output info about server
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task GetServerInfoAsync()
        {
            if (!_userList[curChatIndex].isConnected)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }


            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, "Информация о сервере: " +
                $"\nServerUri: {_userList[curChatIndex].Client.ServerAddress}" +
                $"\nHostName: {_userList[curChatIndex].Client.ServerAddress.Host}" +
                $"\nClientState: {_userList[curChatIndex].Client.State}" +
                $"\nNamespaces: {_userList[curChatIndex].Client.Namespaces.Count}" +
                $"\nKeepAlive: {_userList[curChatIndex].Client.KeepAlive}");
        }

        /// <summary>
        /// Work status and id of them
        /// </summary>
        enum JobStatus
        {
            Started = 3,
            Stopped = 0,
            Error = 4
        }

        /// <summary>
        /// Checking: is node a tag
        /// </summary>
        /// <returns>true if node is tag</returns>
        bool IsTag()
        {
            var browse = new OpcBrowseNode(nodeId: _userList[curChatIndex].Node, degree: OpcBrowseNodeDegree.Self, referenceTypes: new[]
            {
                OpcReferenceType.HasTypeDefinition
            });

            browse.Options = OpcBrowseOptions.IncludeAll;
            var opcNodeInfo = _userList[curChatIndex].Client.BrowseNode(browse);

            var nodeType = opcNodeInfo.Children().ToArray()[0].Reference.DisplayName.ToString();

            if (nodeType != "TagType")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Command bot handler
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        async Task CommandWithArgs()
        {
            if (_userList[curChatIndex].message.Text.Split(" ").Length == 2)
            {
                switch (_userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND])
                {
                    case Commands.GetValueCommand:
                        await GetValueAsync();

                        return;
                    case Commands.SetValueCommand:
                        await SetValueAsync();

                        return;
                    case Commands.ConnectCommand:
                        if (!_userList[curChatIndex].isConnected)
                        {
                            await ConnectMessageAsync();

                            return;
                        }

                        await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotNecessaryConnectMessage);
                        
                        return;
                    case Commands.GetInfoCommand:
                        await GetInfoAsync();

                        return;
                    case Commands.SubscribeOnAlarmCommand:
                        await _userList[curChatIndex].AlarmSubscriptions.subscribeOnAlarmAsync();

                        return;
                    case Commands.UnsubscribeCommand:
                        await _userList[curChatIndex].AlarmSubscriptions.unsubscribeAsync();

                        return;
                }
            }

            if (_userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.UnsubscribeCommand
                || _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.SubscribeOnAlarmCommand
                || _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.GetInfoCommand
                || _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.SetValueCommand
                || _userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.GetValueCommand)
            {
                if (!_userList[curChatIndex].isConnected)
                {
                    await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.NotConnectedMessage);

                    return;
                }

                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongDataMessage);

                return;
            }
            else if (_userList[curChatIndex].message.Text.Split(" ")[User.POS_OF_COMMAND] == Commands.ConnectCommand)
            {
                await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongDataMessage);

                return;
            }

            await botClient.SendTextMessageAsync(_userList[curChatIndex].message.Chat, MessageStrings.WrongCommandMessage);

            return;
        }

    }
}
