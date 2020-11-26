using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class EditLandingData : IDisposable
    {
        private LandingData _editData = new LandingData();
        private LandingData _origData = new LandingData();
        private List<string> _profiles = new List<string>();
        private List<string> _steels = new List<string>();
        private List<string> _gosts = new List<string>();
        private List<string> _customers = new List<string>();
        private List<string> _classes = new List<string>();
        private Shift _shift = new Shift();
        
        // private IConfigurationRoot _config;
        private Logger _logger;
        private DBConnection _db = new DBConnection();
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        
        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            Initialize();
        }

        public void Dispose()
        {
        }

        private void Initialize()
        {
            _editData = _landingService.EditMode ? _landingService.GetEditable() : new LandingData();
            _origData = _landingService.EditMode ? _landingService.GetOriginal() : new LandingData();

            try
            {
                _profiles = _db.GetProfiles();
                _steels = _db.GetSteels();
                _gosts = _db.GetGosts();
                _customers = _db.GetCustomers();
                _classes = _db.GetClasses();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить информацию из справочников [{ex.Message}]");
                _profiles = new List<string>();
                _steels = new List<string>();
                _gosts = new List<string>();
                _customers = new List<string>();
                _classes = new List<string>();
            }

            StateHasChanged();
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

        private async void EditLanding()
        {
            // Проверка на корректность заполнения сечения заготовки
            if (string.IsNullOrEmpty(_editData.IngotProfile))
            {
                _profiles = _db.GetProfiles();
                if (_profiles.Count > 0)
                {
                    _editData.IngotProfile = _profiles[0];
                }
                else
                {
                    _editData.IngotProfile = "0";
                }
            }

            // Проверка на корректность заполнения марки стали
            if (string.IsNullOrEmpty(_editData.SteelMark))
            {
                _steels = _db.GetSteels();
                if (_steels.Count > 0)
                {
                    _editData.SteelMark = _steels[0];
                }
                else
                {
                    _editData.SteelMark = "Не задано";
                }
            }

            // Проверка на корректность заполнения заказчика
            if (string.IsNullOrEmpty(_editData.Customer))
            {
                _customers = _db.GetCustomers();
                if (_customers.Count > 0)
                {
                    _editData.Customer = _customers[0];
                }
                else
                {
                    _editData.Customer = "Не задано";
                }
            }

            // Проверка на корректность заполнения ГОСТа
            if (string.IsNullOrEmpty(_editData.Standart))
            {
                _gosts = _db.GetGosts();
                if (_gosts.Count > 0)
                {
                    _editData.Standart = _gosts[0];
                }
                else
                {
                    _editData.Standart = "Не задано";
                }
            }

            // Проверка на корректность заполнения класса
            if (string.IsNullOrEmpty(_editData.IngotClass))
            {
                _classes = _db.GetClasses();
                if (_classes.Count > 0)
                {
                    _editData.IngotClass = _classes[0];
                }
                else
                {
                    _editData.IngotClass = "Не задано";
                }
            }

            // Проверка на корректность заполнения длины заготовки
            if (_editData.IngotLength == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Длина заготовки]");
                goto finish;
            }

            // Проверка на корректность заполнения веса заготовки
            if (_editData.WeightOne == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Вес заготовки]");
                goto finish;
            }

            // Проверка на корректность заполнения кода продукции
            if (_editData.ProductCode == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Код продукции]");
                goto finish;
            }

            // Проверка на корректность заполнения диаметра
            if ((int) _editData.Diameter == 0)
            {
                ShowMessage(MessageType.Danger, "Не заполнено поле [Диаметр]");
                goto finish;
            }

            // Проверка на корректность заполнения номера бригады
            if (string.IsNullOrEmpty(_editData.Shift))
            {
                _editData.Shift = _shift.GetCurrentShiftNumber().ToString();
            }

            // Проверка на корректность заполнения профиля годной продукции
            if (string.IsNullOrEmpty(_editData.ProductProfile))
            {
                _editData.ProductProfile = "№";
            }

            _editData.WeightAll = _editData.WeightOne * _editData.IngotsCount;
            bool res =_db.EditMelt(_origData, _editData);
            if (!res)
            {
                _logger.Error($"При изменении параметров плавки {_editData.LandingId} возникли ошибки");
            }
            _landingService.ClearEditable();
            await JSRuntime.InvokeAsync<string>("openQuery", null);
            
            finish:
            await Task.Delay(TimeSpan.FromSeconds(5));
            HideMessage();
            StateHasChanged();
        }
    }
}