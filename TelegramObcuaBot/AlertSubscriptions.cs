using Opc.UaFx;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramObcuaBot
{
    internal class AlertSubscriptions
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

        public Queue<string> alertsQueue = new Queue<string>();

        public int nodeSeverity;
        AlertSubscriptions(Message message, ITelegramBotClient botClient, string[] BotCommands, OpcClient opcClient, bool isConnctd)
        {
            this.message = message;
            this.botClient = botClient;
            this.BotCommands = BotCommands;
            _client = opcClient;
            isConnected = isConnctd;
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
                if (subscribeId == _client.Subscriptions[i].Id)
                {
                    _client.Subscriptions[i].Unsubscribe();
                    await botClient.SendTextMessageAsync(message.Chat, MessageStrings.SuccecsfullyUnsubscribedMessage);

                    return;
                }

            }

            await botClient.SendTextMessageAsync(message.Chat, MessageStrings.CannotFindIdMessage);

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
    }
}
