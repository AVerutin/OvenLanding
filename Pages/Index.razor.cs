using System;
using System.Collections.Generic;
using System.Threading;
using NLog;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OvenLanding.Data;
using System.Timers;

namespace OvenLanding.Pages
{
    public partial class Index
    {
        private Logger _logger;
        private IConfigurationRoot _config;
        private static DBConnection _db;
        private bool _connectedToDb;
        private static List<LandingTable> _landed = new List<LandingTable>();
        private System.Timers.Timer _timer;
        private string _message = "";
        
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
            _landed = _db.GetLandingOrder();
            StateHasChanged();
            SetTimer(5);
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

        private int IncLanding(int uid)
        {
            int res = _db.IncLanding(uid);
            _landed = _db.GetLandingOrder();
            StateHasChanged();
            return res;
        }

        private int DecLanding(int uid)
        {
            int res = _db.DecLanding(uid);
            _landed = _db.GetLandingOrder();
            StateHasChanged();
            return res;
        }

        private void SetTimer(int seconds)
        {
            _timer = new System.Timers.Timer(seconds * 1000);
            _timer.Elapsed += UpdateData;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void UpdateData(Object source, ElapsedEventArgs e)
        {
            _landed = _db.GetLandingOrder();
            _message = "updated";
            StateHasChanged();
        }
    }
}