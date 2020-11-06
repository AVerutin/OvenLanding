using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OvenLanding.Data;
using System.Timers;

namespace OvenLanding.Pages
{
    public partial class Index : IDisposable
    {
        private Logger _logger;
        private IConfigurationRoot _config;
        private static DBConnection _db;
        private bool _connectedToDb;
        private static List<LandingData> _landed = new List<LandingData>();
        private Timer _timer;
        
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        private string _semaphoreColor = "darkcyan";
        
        protected override async void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _landingService.PropertyChanged += UpdateMessage;
            await Initialize();
        }

        public void Dispose()
        {
            _landingService.PropertyChanged -= UpdateMessage;
            _db.Close();
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

        private async Task Initialize()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            int reconnect = Int32.Parse(_config.GetSection("DBConnection:Reconnect").Value);
            
            await ConnectToDb(reconnect);
            try
            {
                _landed = _db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }

            StateHasChanged();
            SetTimer(3);
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
                // DateTime now = DateTime.Now;
                // string msg = String.Format("[{0:G}] => {1}", now, "Подключение к БД установлено");
                // _logger.Info(msg);
                res = true;
            }

            return res;
        }

        private void IncLanding(int uid)
        {
            int res = -1;
            try
            {
                res = _db.IncLanding(uid);
                _landed = _db.GetLandingOrder();
                _logger.Info($"Добавлена заготовка в плавку [{uid}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось добавить заготовку в плавку [{uid}] => {ex.Message}");
            }
            StateHasChanged();
        }

        private void DecLanding(int uid)
        {
            int res = -1;
            try
            {
                res = _db.DecLanding(uid);
                _landed = _db.GetLandingOrder();
                _logger.Info($"Удалена заготовка из плавки [{uid}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось добавить заготовку в плавку [{uid}] => {ex.Message}");
            }

            StateHasChanged();
        }

        /// <summary>
        /// Перемещение текущей плавки вверх (дальше от печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private void MoveUp(int uid)
        {
            int cnt = 0;
            int currPosition = 0;
            
            List<LandingData> oldOrder;
            List<LandingData> order = new List<LandingData>();

            try
            {
                oldOrder = _db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"[MeltMoveUp] => Не удалось получить текущий порядок плавок [{ex.Message}]");
                return;
            }

            foreach (LandingData item in oldOrder)
            {
                order.Add(item);
                if (item.LandingId == uid)
                {
                    currPosition = cnt;
                }
                cnt++;
            }

            if (currPosition != 0)
            {
                for (int i = 0; i < oldOrder.Count; i++)
                {
                    if (oldOrder[i].Weighted > 0)
                    {
                        continue;
                    }
                    
                    if (i == currPosition - 1)
                    {
                        order[i] = oldOrder[currPosition];
                        continue;
                    }

                    if (i == currPosition)
                    {
                        order[i] = oldOrder[currPosition - 1];
                        continue;
                    }

                    order[i] = oldOrder[i];
                }

                ClearCurrentOrder(oldOrder);
                SetNewOrder(order);
            }
            else
            {
                _logger.Error($"Плавка [{uid}] находится последней в очереди, некуда поднимать");
            }
        }
        
        /// <summary>
        /// Перемещение текущей плавки вниз (ближе к печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private void MoveDown(int uid)
        {
            int cnt = 0;
            int currPosition = 0;
            
            List<LandingData> oldOrder;
            List<LandingData> order = new List<LandingData>();

            try
            {
                oldOrder = _db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"[MeltMoveUp] => Не удалось получить текущий порядок плавок [{ex.Message}]");
                return;
            }

            foreach (LandingData item in oldOrder)
            {
                order.Add(item);
                if (item.LandingId == uid)
                {
                    currPosition = cnt;
                }
                cnt++;
            }

            if (currPosition != oldOrder.Count - 1)
            {
                for (int i = 0; i < oldOrder.Count; i++)
                {
                    if (oldOrder[i].Weighted > 0)
                    {
                        continue;
                    }
                    
                    if (i == currPosition)
                    {
                        order[i] = oldOrder[currPosition + 1];
                        continue;
                    }

                    if (i == currPosition + 1)
                    {
                        order[i] = oldOrder[currPosition];
                        continue;
                    }

                    order[i] = oldOrder[i];
                }

                ClearCurrentOrder(oldOrder);
                SetNewOrder(order);
            }
            else
            {
                _logger.Error($"Плавка [{uid}] находится первой в очереди, некуда опускать");
            }
        }

        /// <summary>
        /// Очистить текущую очередь на посаде печи
        /// </summary>
        private void ClearCurrentOrder(List<LandingData> order)
        {
            int i = 1;
            foreach (LandingData melt in order)
            {
                if (melt.Weighted == 0)
                {
                    try
                    {
                        _db.Remove(melt.LandingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[{i}] Не удалось удалить плавку [{melt.LandingId}] => {ex.Message}");
                    }
                }

                i++;
            }

            try
            {
                _landed = _db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось обновить список плавок после очистки очереди [{ex.Message}]");
            }

            StateHasChanged();
        }

        /// <summary>
        /// Установить новую очередь на посаде печи
        /// </summary>
        /// <param name="order">Плавка для постановки в очередь</param>
        private void SetNewOrder(List<LandingData> order)
        {
            for (int i = order.Count-1; i >= 0; i--)
            {
                if (order[i].Weighted == 0)
                {
                    try
                    {
                        _db.CreateOvenLanding(order[i]);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Не удалось добавить плавку {order[i].LandingId} в очередь [{ex.Message}]");
                    }
                }
            }

            StateHasChanged();
        }

        private async Task EditLanding(int uid)
        {
            LandingData edit = new LandingData();
            foreach (LandingData item in _landed)
            {
                if (item.LandingId == uid)
                {
                    edit = item;
                    break;
                }
            }
            
            _landingService.SetEditable(edit);
            // _landingService;
            // object[] quoteArray = { };
            await JSRuntime.InvokeAsync<string>("openEditor", null);
        }

        private void Remove(int uid)
        {
            try
            {
                _db.Remove(uid);
                try
                {
                    _landed = _db.GetLandingOrder();
                }
                catch (Exception e)
                {
                    _logger.Error($"Не удалось удалить плавку {uid} [{e.Message}]");
                }

                _logger.Info($"Удалена плавка [{uid}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось удалить плавку [{uid}] => {ex.Message}");
            }

            StateHasChanged();
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
            try
            {
                _landed = _db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                // _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }

            _landingService.IngotsCount = DateTime.Now.Millisecond;
            if (_semaphoreColor == "darkcyan")
            {
                _semaphoreColor = "darkgrey";
            }
            else
            {
                _semaphoreColor = "darkcyan";
            }
        }

        private async void UpdateMessage(object sender, PropertyChangedEventArgs args)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
}