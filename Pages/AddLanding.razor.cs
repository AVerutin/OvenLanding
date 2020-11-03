using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class AddLanding : IDisposable
    {
        private LandingData _landingData = new LandingData();
        private ProfileData _profileData = new ProfileData();
        private SteelData _steelData = new SteelData();
        private GostData _gostData = new GostData();
        private CustomerData _customerData = new CustomerData();
        private ClassData _classData = new ClassData();
        private List<string> _profiles = new List<string>();
        private List<string> _steels = new List<string>();
        private List<string> _gosts = new List<string>();
        private List<string> _customers = new List<string>();
        private List<string> _classes = new List<string>();

        private Logger _logger;
        private DBConnection _db;
        private bool _connectedToDb;
        private IConfigurationRoot _config;
        private string _showWindowAddProfile = "none";
        private string _showWindowAddSteel = "none";
        private string _showWindowAddGost = "none";
        private string _showWindowAddCustomer = "none";
        private string _showWindowAddClass = "none";

        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";

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
            UpdateDictionaries();
            
            try
            {
                _landingData = _landingService.GetState();
            }
            catch (Exception ex)
            {
                _landingData = new LandingData();
                _logger.Error($"Не удалось получить предыдущее состояние полей ввода [{ex.Message}]");
            }
            StateHasChanged();
        }
        
        public void Dispose()
        {
            _landingService.SaveState(_landingData);
            _db.Close();
        }

        /// <summary>
        /// Обновить данные из справочников
        /// </summary>
        private void UpdateDictionaries()
        {
            try
            {
                _profiles = _db.GetProfiles();
            }
            catch (Exception ex)
            {
                _profiles = new List<string>();
                _logger.Error($"Не удалось получить список профилей заготовки [{ex.Message}]");
            }

            try
            {
                _steels = _db.GetSteels();
            }
            catch (Exception ex)
            {
                _steels = new List<string>();
                _logger.Error($"Не удалось получить список марок стали [{ex.Message}]");
            }

            try
            {
                _gosts = _db.GetGosts();
            }
            catch (Exception ex)
            {
                _gosts = new List<string>();
                _logger.Error($"Не удалось получить список ГОСТов [{ex.Message}]");
            }

            try
            {
                _customers = _db.GetCustomers();
            }
            catch (Exception ex)
            {
                _customers = new List<string>();
                _logger.Error($"Не удалось получить список заказчиков [{ex.Message}]");
            }

            try
            {
                _classes = _db.GetClasses();
            }
            catch (Exception ex)
            {
                _classes = new List<string>();
                _logger.Error($"Не удалось получить список классов [{ex.Message}]");
            }
        }

        /// <summary>
        /// Добавить плавку в очередь на посад печи
        /// </summary>
        private async void AddNewLanding()
        {
            if(!string.IsNullOrEmpty(_landingData.MeltNumber))
            {
                _logger.Info($"Номер плавки: {_landingData.MeltNumber}");
                _landingData.WeightAll = _landingData.WeightOne * _landingData.IngotsCount;

                // Проверка на корректность заполнения сечения заготовки
                if (string.IsNullOrEmpty(_landingData.IngotProfile))
                {
                    _profiles = _db.GetProfiles();
                    if (_profiles.Count > 0)
                    {
                        _landingData.IngotProfile = _profiles[0];
                    }
                    else
                    {
                        _landingData.IngotProfile = "Не задано";
                    }
                }

                // Проверка на корректность заполнения марки стали
                if (string.IsNullOrEmpty(_landingData.SteelMark))
                {
                    _steels = _db.GetSteels();
                    if (_steels.Count > 0)
                    {
                        _landingData.SteelMark = _steels[0];
                    }
                    else
                    {
                        _landingData.SteelMark = "Не задано";
                    }
                }

                // Проверка на корректность заполнения заказчика
                if (string.IsNullOrEmpty(_landingData.Customer))
                {
                    _customers = _db.GetCustomers();
                    if (_customers.Count > 0)
                    {
                        _landingData.Customer = _customers[0];
                    }
                    else
                    {
                        _landingData.Customer = "Не задано";
                    }
                }

                // Проверка на корректность заполнения ГОСТа
                if (string.IsNullOrEmpty(_landingData.Gost))
                {
                    _gosts = _db.GetGosts();
                    if (_gosts.Count > 0)
                    {
                        _landingData.Gost = _gosts[0];
                    }
                    else
                    {
                        _landingData.Gost = "Не задано";
                    }
                }

                // Проверка на корректность заполнения класса
                if (string.IsNullOrEmpty(_landingData.Class))
                {
                    _classes = _db.GetClasses();
                    if (_classes.Count > 0)
                    {
                        _landingData.Class = _classes[0];
                    }
                    else
                    {
                        _landingData.Class = "Не задано";
                    }
                }
                
                // Проверка на корректность заполнения смены
                if (string.IsNullOrEmpty(_landingData.Shift))
                {
                    _landingData.Shift = "Не задано";
                }

                int uid = _db.CreateOvenLanding(_landingData);

                if (uid == -1)
                {
                    string message = $"Ошибка при добавлении плавки №{_landingData.MeltNumber} в базу данных";
                    _logger.Error(message);
                    ShowMessage(MessageType.Danger, message);
                }
                else
                {
                    string message = $"Добавлена плавка №{_landingData.MeltNumber} - UID = {uid}";
                    _logger.Info(message);
                    ShowMessage(MessageType.Success, $"Добавлена плавка №{_landingData.MeltNumber}");
                }

                // _landingData = new LandingData();
                _landingData.MeltNumber = "";
                _landingData.IngotsCount = 0;
                UpdateDictionaries();
                
                await Task.Delay(TimeSpan.FromSeconds(5));
                HideMessage();
                StateHasChanged();
            }
        }

        // Profile
        private void ShowProfile()
        {
            _profileData.ProfileName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddGost = "none";
            _showWindowAddSteel = "none";
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
                StateHasChanged();
            }
            else
            {
                // Не добавили профиль заготовки
                _logger.Error($"Не удалось добавить профиль заготовки{profileName}");
                StateHasChanged();
            }

            _showWindowAddProfile = "none";
        }

        // Steel
        private void ShowSteel()
        {
            // Прячем все остальные формы
            _steelData.SteelName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "block";
        }
        
        private void AddSteel()
        {
            
            string steelName = _steelData.SteelName ?? "";
            if (_db.AddSteel(steelName))
            {
                // Добавили марку стали
                _steels = _db.GetSteels();
                _logger.Info($"Добавлена марка стали{steelName}");
                StateHasChanged();
            }
            else
            {
                // Не добавили марку стали
                _logger.Error($"Не удалось добавить марку стали{steelName}");
                StateHasChanged();
            }

            _showWindowAddSteel = "none";
        }
        
        // GOST
        private void ShowGost()
        {
            _gostData.GostName = "";
            _showWindowAddClass = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddGost = "block";
        }

        private void AddGost()
        {
            string gostName = _gostData.GostName ?? "";
            if (_db.AddGost(gostName))
            {
                // Добавили профиль заготовки
                _gosts = _db.GetGosts();
                _logger.Info($"Добавлен ГОСТ{gostName}");
                StateHasChanged();
            }
            else
            {
                // Не добавили профиль заготовки
                _logger.Error($"Не удалось добавить ГОСТ{gostName}");
                StateHasChanged();
            }

            _showWindowAddGost = "none";
        }
        
        // Customer
        private void ShowCustomer()
        {
            _customerData.Customer = "";
            _showWindowAddClass = "none";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddCustomer = "block";

        }

        private void AddCustomer()
        {
            string customerName = _customerData.Customer ?? "";
            if (_db.AddCustomer(customerName))
            {
                // Добавили профиль заготовки
                _customers = _db.GetCustomers();
                _logger.Info($"Добавлен заказчик{customerName}");
                StateHasChanged();
            }
            else
            {
                // Не добавили профиль заготовки
                _logger.Error($"Не удалось добавить заказчика{customerName}");
                StateHasChanged();
            }

            _showWindowAddCustomer = "none";
        }
        
        // Class
        private void ShowClass()
        {
            _classData.Class = "";
            _showWindowAddGost = "none";
            _showWindowAddProfile = "none";
            _showWindowAddSteel = "none";
            _showWindowAddCustomer = "none";
            _showWindowAddClass = "block";
        }

        private void AddClass()
        {
            string className = _classData.Class ?? "";
            if (_db.AddClass(className))
            {
                // Добавили профиль заготовки
                _classes = _db.GetClasses();
                _logger.Info($"Добавлен класс{className}");
                StateHasChanged();
            }
            else
            {
                // Не добавили профиль заготовки
                _logger.Error($"Не удалось добавить класс{className}");
                StateHasChanged();
            }

            _showWindowAddClass = "none";
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
                // DateTime now = DateTime.Now;
                // string msg = String.Format("[{0:G}] => {1}", now, "Подключение к БД установлено");
                // _logger.Info(msg);
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
    }
}