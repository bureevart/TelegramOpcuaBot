using Opc.UaFx.Client;
using System;
using System.Linq;
using Telegram.Bot.Types;

namespace TelegramOpcuaBot
{
    /// <summary>
    /// Класс пользователя
    /// </summary>
    internal class User
    {
        public int Id { get; set; }
        public const int POS_OF_COMMAND_PARAMS = 1;
        public const int POS_OF_COMMAND = 0;
        public const int NUMBER_OF_PARAMS_IN_CONNECTION = 3;
        public const char PARAMS_SEPARATOR = '!';

        public long id;
        internal Message message;
        internal bool isConnected = false;
        private OpcClient _client;

        private string _ip;
        private string _login;
        private string _password;

        private string _node;

        private AlarmSubscriptions _alarmSubscriptions;

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
        public OpcClient Client 
        { 
            get { return _client; } 
            set { _client = value; }
        }
        public string Node
        {
            get { return _node; } 
            set { _node = value; } 
        }
        public AlarmSubscriptions AlarmSubscriptions 
        {
            get { return _alarmSubscriptions; }
            set{ _alarmSubscriptions = value; }
        }
        public string Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }
        public string Login
        {
            get { return _login; }
            set { _login = value; }
        }
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }
    }
}
