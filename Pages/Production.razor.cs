using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using System.Threading.Tasks;
using OvenLanding.Data;
using System.Timers;
using OvenLanding.Annotations;

namespace OvenLanding.Pages
{
    public partial class Production : IDisposable
    {
        private Logger _logger;
        private static readonly DBConnection Db = new DBConnection();
        private static List<LandingData> _landed = new List<LandingData>();
        private static List<LandingData> _window1 = new List<LandingData>(); // Список плавок перед печью
        private static List<LandingData> _window2 = new List<LandingData>(); // Список плавок, садящихся в печь
        private static List<LandingData> _window3 = new List<LandingData>(); // Список взвешивающихся плавок

        private Timer _timer;
        
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        private string _semaphoreColor = "darkcyan";
        private string _selectRow = "none";
        
        protected override void OnInitialized()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _landingService.PropertyChanged += UpdateMessage;
            Initialize();
        }

        public void Dispose()
        {
            _landingService.PropertyChanged -= UpdateMessage;
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

        private async void Initialize()
        {
            // Получение очереди плавок
            try
            {
                _landed = await GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }
            
            StateHasChanged();
            SetTimer(5);
        }

        /// <summary>
        /// Получить список плавок в очереди
        /// </summary>
        /// <returns></returns>
        private async Task<List<LandingData>> GetLandingOrder()
        {
            // 1. Получить список плавок 
            // 2. Для каждой плавки из списка получить количество возвратов
            // 3. Для каждой плавки из списка получить количество браков
            // 4. Для каждой плавки из списка получить количество заготовок на поде печи
            // 5. Для каждой плавки из списка получить количество заготовок, выданных из печи
            // 6. Для каждой плавки из списка получить количество прокатанных заготвок 
            
            List<LandingData> result = Db.GetLandingOrder();
            
            // Получение данных по каждой плавке
            foreach (LandingData item in result)
            {
                // Количество взвешенных заготовок
                WeightedIngotsCount weighted = Db.GetWeightedIngotsCount(item.LandingId);
                item.WeightedIngots = weighted.WeightedCount;
                
                // Количество возвратов
                List<ReturningData> returns = Db.GetReturns(item.MeltNumber);
                if (returns.Count > 0)
                {
                    foreach (ReturningData t in returns)
                    {
                        item.IngotsReturned += t.IngotsCount;
                    }
                }

                await Task.Delay(200);
            }

            // Разнесение плавко по разным окнам
            _window1 = new List<LandingData>();
            _window2 = new List<LandingData>();
            _window3 = new List<LandingData>();
            
            foreach (LandingData item in _landed)
            {
                if (item.WeightedIngots == 0)
                {
                    _window1.Add(item);
                }
                else if (item.Weighted == 0)
                {
                    _window2.Add(item);
                }
                else
                {
                    _window3.Add(item);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Установить таймер за данный интервал времени
        /// </summary>
        /// <param name="seconds">Время срабатывания таймера</param>
        private void SetTimer(int seconds)
        {
            _timer = new Timer(seconds * 1000);
            _timer.Elapsed += UpdateData;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async void UpdateData(Object source, ElapsedEventArgs e)
        {
            try
            {
                _landed = await GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
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