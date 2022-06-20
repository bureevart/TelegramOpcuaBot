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
    /// Класс подписок на алармы
    /// </summary>
    public class AlarmSubscriptions
    {
        public const int POS_OF_COMMAND_PARAMS = 1;

        Message message;
        internal ITelegramBotClient botClient;
        internal static bool isConnected = false;
        private static OpcClient _client;

        public Queue<string> alertsQueue = new Queue<string>();

        public int nodeSeverity;

        /// <summary>
        /// Конструктор алармов
        /// </summary>
        /// <param name="message">сообщение, отправленное пользователем боту</param>
        /// <param name="botClient">телеграм-бот</param>
        /// <param name="opcClient">клиент opc</param>
        /// <param name="isConnctd">есть ли подключение к серверу</param>
        internal AlarmSubscriptions(Message message, ITelegramBotClient botClient, OpcClient opcClient, bool isConnctd)
        {
            this.message = message;
            this.botClient = botClient;
            _client = opcClient;
            isConnected = isConnctd;
        }

        /// <summary>
        /// Проверка подписок на алармы
        /// </summary>
        /// <returns>Task (вызывающий код будет ждать завершение метода)</returns>
        internal async Task checkSubscriptionsAsync()
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
        /// Отписка от подписки с указанным id
        /// </summary>
        /// <returns>Task (вызывающий код будет ждать завершение метода)</returns>
        internal async Task unsubscribeAsync()
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
        /// Подписка на аларм (с указанием severity)
        /// </summary>
        /// <returns>Task (вызывающий код будет ждать завершение метода)</returns>
        internal async Task subscribeOnAlarmAsync()
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
        /// Обработчик алармов
        /// </summary>
        /// <param name="sender">отправитель</param>
        /// <param name="e">событие, которое произошло</param>
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

        /// <summary>
        /// Вывод алармов пользователю
        /// </summary>
        public async void sendAlertAsync()
        {
            while (alertsQueue.Count > 0)
            {
                var alert = alertsQueue.Dequeue();
                await botClient.SendTextMessageAsync(message.Chat, $"Обнаружен аларм тяжестью равного {nodeSeverity} или выше: \n{alert}");
            }
        }

    }
}


// xml документация
// руководство администрирования (настройка, механизм ввода токена)
// руководство пользователя
// разделители (setNode пробелы......)