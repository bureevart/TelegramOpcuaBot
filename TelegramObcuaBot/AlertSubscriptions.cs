using Opc.UaFx;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramOpcuaBot
{
    /// <summary>
    /// Class of subsctiptions on alarms
    /// </summary>
    public class AlarmSubscriptions
    {
        /// <summary>
        /// Index of position command parameters in input
        /// </summary>
        public const int POS_OF_COMMAND_PARAMS = 1;

        Message message;
        internal ITelegramBotClient botClient;
        internal static bool isConnected = false;
        private static OpcClient _client;

        /// <summary>
        /// Queue of registered alarms
        /// </summary>
        public Queue<string> alarmsQueue = new Queue<string>();

        /// <summary>
        /// Severity of node
        /// </summary>
        public int nodeSeverity;

        /// <summary>
        /// Alarms constructor
        /// </summary>
        /// <param name="message">message, sended bot by user</param>
        /// <param name="botClient">telegram bot</param>
        /// <param name="opcClient">client</param>
        /// <param name="isConnctd">is Connected to server</param>
        internal AlarmSubscriptions(Message message, ITelegramBotClient botClient, OpcClient opcClient, bool isConnctd)
        {
            this.message = message;
            this.botClient = botClient;
            _client = opcClient;
            isConnected = isConnctd;
        }

        /// <summary>
        /// Checking alarm on subscriptions
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        internal async Task CheckSubscriptionsAsync()
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

        /// <summary>
        /// Unsubscribe from a subscription with the specified id
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        internal async Task UnsubscribeAsync()
        {
            if (!isConnected)
            {
                await botClient.SendTextMessageAsync(message.Chat, MessageStrings.NotConnectedMessage);

                return;
            }

            var subId = message.Text.Split(" ")[POS_OF_COMMAND_PARAMS];

            var subscribeId = -1;
            try
            {
                subscribeId = int.Parse(subId);
            }
            catch (Exception)
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

        /// <summary>
        /// Subsctiption on alarm (necessary severity)
        /// </summary>
        /// <returns>Task (called code will wait end of method)</returns>
        internal async Task SubscribeOnAlarmAsync()
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
            catch (Exception)
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

        /// <summary>
        /// Alarms handler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">received event</param>
        private void HandleGlobalEvents(object sender, OpcEventReceivedEventArgs e)
        {
            alarmsQueue.Enqueue(
                $"Источник {e.Event.SourceName}" +
                $"\nId ноды: {e.Event.SourceNodeId}" +
                $"\nСообщение: {e.Event.Message}" +
                $"\nSeverity: {e.Event.Severity}" +
                $"\nВремя получения: {e.Event.ReceiveTime}");

            SendAlertAsync();
        }

        /// <summary>
        /// alarm output
        /// </summary>
        public async void SendAlertAsync()
        {
            while (alarmsQueue.Count > 0)
            {
                var alert = alarmsQueue.Dequeue();
                await botClient.SendTextMessageAsync(message.Chat, $"Обнаружен аларм тяжестью равного {nodeSeverity} или выше: \n{alert}");
            }
        }

    }
}