using System;
using System.Collections.Generic;
using NLog;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class Index
    {
        private LandingData _landingData = new LandingData();
        private ProfileData _profileData = new ProfileData();
        private SteelData _steelData = new SteelData();
        
        private IConfigurationRoot _config;
        private Logger _logger;
        private DBConnection _db;
        private bool _connectedToDb;
        private string _message = "";
        private string _show = "none";
        private const string StateOk = "alert alert-success";
        private const string StateError = "alert alert-danger";
        private Dictionary<int, string> _profiles = new Dictionary<int, string>();
        private Dictionary<int, string> _steels = new Dictionary<int, string>();
        private List<LandingTable> _landed = new List<LandingTable>();
        
        private string _state = StateOk;
        private string _showWindowAddProfile = "none";
        private string _showWindowAddSteel = "none";
        
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
            _profiles = _db.GetProfiles();
            _steels = _db.GetSteels();
            _landed = _db.GetLandingOrder();
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
                _message = msg;
                _show = "block";
                _state = StateError;
                res = false;
            }
            else
            {
                DateTime now = DateTime.Now;
                string msg = String.Format("[{0:G}] => {1}", now, "Подключение к БД установлено");
                _message = msg;
                _show = "block";
                _state = StateOk;
                res = true;
            }

            return res;
        }

        private void AddLanding()
        {
            _logger.Info($"Номер плавки: {_landingData.Number}");
            string query =
                String.Format("INSERT INTO oven_landing (plav_number, order_num, steel_mark, profile, legal_count, legal_weight, length, weight) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})", 
                    _landingData.Number, 
                    _landingData.OrderNum,
                    int.Parse(_landingData.SteelMark), 
                    int.Parse(_landingData.IngotProfile), 
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
            
            _landed = _db.GetLandingOrder();
            StateHasChanged();
        }

        private void ShowProfile()
        {
            _showWindowAddProfile = "block";
        }

        private void AddProfile()
        {
            string profileName = _profileData.ProfileName ?? "";
            if (_db.AddProfile(profileName))
            {
                // Добавили профиль заготовки
                _profiles = _db.GetProfiles();
                _logger.Info($"Добавлен профиль заготовки{profileName}");
                _message = $"Добавлен профиль заготовки{profileName}";
                _state = StateOk;
                _show = "block;";
                StateHasChanged();
            }
            else
            {
                // Не добавили профиль заготовки
                _logger.Error($"Не удалось добавить профиль заготовки{profileName}");
                _message = $"Не удалось добавить профиль заготовки{profileName}";
                _state = StateError;
                _show = "block;";
                StateHasChanged();
            }

            _showWindowAddProfile = "none";
        }

        private void AddSteel()
        {
            
            string steelName = _steelData.SteelName ?? "";
            if (_db.AddSteel(steelName))
            {
                // Добавили марку стали
                _steels = _db.GetSteels();
                _logger.Info($"Добавлена марка стали{steelName}");
                _message = $"Добавлена марка стали{steelName}";
                _state = StateOk;
                _show = "block;";
                StateHasChanged();
            }
            else
            {
                // Не добавили марку стали
                _logger.Error($"Не удалось добавить марку стали{steelName}");
                _message = $"Не удалось добавить марку стали{steelName}";
                _state = StateError;
                _show = "block;";
                StateHasChanged();
            }

            _showWindowAddSteel = "none";
        }

        private void ShowSteel()
        {
            _showWindowAddSteel = "block";
        }
    }
}