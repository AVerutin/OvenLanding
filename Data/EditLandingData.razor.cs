using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;

namespace OvenLanding.Data
{
    public partial class EditLandingData : IDisposable
    {
        private LandingTable _editData;
        private List<string> _profiles = new List<string>();
        private List<string> _steels = new List<string>();
        private List<string> _gosts = new List<string>();
        private List<string> _customers = new List<string>();
        private List<string> _classes = new List<string>();
        
        private IConfigurationRoot _config;
        private Logger _logger;
        private DBConnection _db;
        private bool _connectedToDb;
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        
        protected override async void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            await Initialize();
        }

        public void Dispose()
        {
            _db.Close();
        }

        private async Task Initialize()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            int reconnect = Int32.Parse(_config.GetSection("DBConnection:Reconnect").Value);
            
            await ConnectToDb(reconnect);
            _editData = _landingService.EditableDate;
            _profiles = _db.GetProfiles();
            _steels = _db.GetSteels();
            _gosts = _db.GetGosts();
            _customers = _db.GetCustomers();
            _classes = _db.GetClasses();
            StateHasChanged();
        }
        
        private async Task ConnectToDb(int reconnect)
        {
            while (!_connectedToDb)
            {
                if(!TryConnectToDb())
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(reconnect));
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
                StateHasChanged();
            }
        }

        private bool TryConnectToDb()
        {
            bool res;
            _db = new DBConnection();
            _connectedToDb = _db.DbInit();

            if(!_connectedToDb)
            {
                DateTime now = DateTime.Now;
                string msg = String.Format("[{0:G}] => {1}", now, "Не удалось подключиться к базе данных");
                _logger.Error(msg);
                res = false;
            }
            else
            {
                DateTime now = DateTime.Now;
                string msg = String.Format("[{0:G}] => {1}", now, "Подключение к БД установлено");
                _logger.Info(msg);
                res = true;
            }

            return res;
        }
        
        private void ShowMessage(MessageType type, string message)
        {
            _message = message ?? "";
            switch (type)
            {
                case MessageType.Primary: _messageClass = "alert alert-primary"; break;
                case MessageType.Secondary: _messageClass = "alert alert-secondary"; break;
                case MessageType.Success: _messageClass = "alert alert-success"; break;
                case MessageType.Danger: _messageClass = "alert alert-danger"; break;
                case MessageType.Warning: _messageClass = "alert alert-warning"; break;
                case MessageType.Info: _messageClass = "alert alert-info"; break;
                case MessageType.Light: _messageClass = "alert alert-light"; break;
                case MessageType.Dark: _messageClass = "alert alert-dark"; break;
            }

            _messageVisible = "block";
            StateHasChanged();
        }

        private void HideMessage()
        {
            _message = "";
            _messageVisible = "none";
            StateHasChanged();
        }

        private void EditLanding()
        {
            
        }

    }
}