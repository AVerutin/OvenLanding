using System;
using NLog;
using System.Threading.Tasks;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class Index
    {
        private LandingData _landingData = new LandingData();
        private Logger _logger;
        private DBConnection _db;
        private string _message = "";
        private string _show = "none";
        private string _state = "alert alert-success";
        
        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            Initialize();
        }

        private void Initialize()
        {
            _db = new DBConnection();
            _db.DbInit();
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
                _state = "alert alert-danger";
                _show = "block;";
            }
            else
            {
                _logger.Info($"Добавлена плавка №{_landingData.Number}");
                _message = $"Добавлена плавка №{_landingData.Number}";
                _state = "alert alert-success";
                _show = "block;";
                _landingData = new LandingData();
            }
        }
    }
}