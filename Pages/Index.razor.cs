using System;
using NLog;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class Index
    {
        private LandingData _landingData = new LandingData();
        private IConfigurationRoot _config;
        private Logger _logger;
        private DBConnection _db;
        private bool _connectedToDb;
        private string _message = "";
        private string _show = "none";
        private const string StateOk = "alert alert-success";
        private const string StateError = "alert alert-danger";
        
        private string _state = StateOk;
        
        protected override async void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            await Initialize();
        }

        private async Task Initialize()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            int reconnect = Int32.Parse(_config.GetSection("DBConnection:Reconnect").Value);
            
            await ConnectToDb(reconnect);
        }

        private async Task ConnectToDb(int reconnect)
        {
            while (!_connectedToDb)
            {
                TryConnectToDb();
                await Task.Delay(TimeSpan.FromMilliseconds(reconnect));
                StateHasChanged();
            }
        }

        private void TryConnectToDb()
        {
            _db = new DBConnection();
            _connectedToDb = _db.DbInit();

            if(!_connectedToDb)
            {
                DateTime now = DateTime.Now;
                string msg = String.Format("[{0:G}] => {1}", now, "Не удалось подключиться к базе данных");
                _message = msg;
                _show = "block";
                _state = StateError;
            }
            else
            {
                DateTime now = DateTime.Now;
                string msg = String.Format("[{0:G}] => {1}", now, "Подкючение к БД установлено");
                _message = msg;
                _show = "block";
                _state = StateOk;
            }
        }

        private void AddLanding()
        {
            _logger.Info($"Номер плавки: {_landingData.Number}");
            string query =
                String.Format("INSERT INTO oven_landing (plav_number, steel_mark, profile, legal_count, legal_weight, length, weight) VALUES ({0}, \'{1}\', \'{2}\', {3}, {4}, {5}, {6})", 
                    _landingData.Number, 
                    _landingData.SteelMark ?? "", 
                    _landingData.IngotProfile ?? "", 
                    _landingData.LegalCount, 
                    _landingData.LegalWeight, 
                    _landingData.Lenght, 
                    _landingData.Weight);
            if (!_db.WriteData(query))
            {
                _logger.Error($"Ошибка при добавлении плавки №{_landingData.Number} в базу данных");
                _message = $"Ошибка при добавлении плавки №{_landingData.Number} в базу данных!";
                _state = StateError;
                _show = "block;";
            }
            else
            {
                _logger.Info($"Добавлена плавка №{_landingData.Number}");
                _message = $"Добавлена плавка №{_landingData.Number}";
                _state = StateOk;
                _show = "block;";
                _landingData = new LandingData();
            }
        }
    }
}