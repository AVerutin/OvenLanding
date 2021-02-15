using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using System.Threading.Tasks;
using OvenLanding.Data;
using System.Timers;

namespace OvenLanding.Pages
{
    public partial class Index : IDisposable
    {
        private Logger _logger;
        private static readonly DBConnection Db = new DBConnection();
        private static List<LandingData> _landed = new List<LandingData>();
        
        private static readonly CoilData CoilData = new CoilData();
        private Timer _timer;
        
        private string _message = "";
        private string _messageClass = "";
        private string _messageVisible = "none";
        private string _semaphoreColor = "#1861ac";
        private string _selectRow = "none";
        private string _loading = "hidden;";
        
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
        
        private void _setLoading(bool visible)
        {
            _loading = visible ? "visible;" : "hidden;";
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
            _setLoading(true);
            _landed = new List<LandingData>();
            await Task.Delay(100);
            
            try
            {
                // _landed = Db.GetLandingOrder();
                _landed = await GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }
            
            _setLoading(false);
            StateHasChanged();
            SetTimer(5);
        }

        private async void IncLanding(int uid)
        {
            try
            {
                Db.IncLanding(uid);
                // _landed = Db.GetLandingOrder();
                _landed = await GetLandingOrder();
                _logger.Info($"Добавлена заготовка в плавку [{uid}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось добавить заготовку в плавку [{uid}] => {ex.Message}");
            }
            StateHasChanged();
        }

        private async void DecLanding(int uid)
        {
            try
            {
                Db.DecLanding(uid);
                // _landed = Db.GetLandingOrder();
                _landed = await GetLandingOrder();
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
        private async void MoveUp(int uid)
        {
            int cnt = 0;
            int currPosition = 0;

            List<LandingData> oldOrder;
            List<LandingData> order = new List<LandingData>();

            try
            {
                // oldOrder = Db.GetLandingOrder();
                oldOrder = await GetLandingOrder();
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

            if (currPosition != 0 && oldOrder[currPosition - 1].Weighted == 0)
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

                int oldCnt = _landed.Count;
                int newCnt;
                do
                {
                    ClearCurrentOrder();
                    SetNewOrder(order);
                    // List<LandingData> tmpOrder = Db.GetLandingOrder();
                    List<LandingData> tmpOrder = await GetLandingOrder();
                    newCnt = tmpOrder.Count;
                } while (oldCnt != newCnt);

                // Получить новую очередь плавок
                // Если количество плавок в новой очереди не равно количеству плавок в заданной очереди
                // Очистить текущую очередь
                // Заполнить новую очередь
            }
            else
            {
                _logger.Error($"Плавка [{uid}] находится последней в очереди, некуда поднимать");
            }
        }

        /// <summary>
        /// Получить список плавок в очереди
        /// </summary>
        /// <returns></returns>
        private async Task<List<LandingData>> GetLandingOrder()
        {
            List<LandingData> result = Db.GetLandingOrder();

            // Проверка на наличие возвожности удаления плавки
            foreach (LandingData item in result)
            {
                WeightedIngotsCount weighted = Db.GetWeightedIngotsCount(item.LandingId);
                item.WeightedIngots = weighted.WeightedCount;

                if (item.Weighted == 0 && item.WeightedIngots == 0)
                {
                    item.CanBeDeleted = true;
                }
                
                await Task.Delay(500);
            }
            
            return result;
        }
        
        /// <summary>
        /// Перемещение текущей плавки вниз (ближе к печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private async void MoveDown(int uid)
        {
            int cnt = 0;
            int currPosition = 0;
            
            List<LandingData> oldOrder;
            List<LandingData> order = new List<LandingData>();

            try
            {
                // oldOrder = Db.GetLandingOrder();
                oldOrder = await GetLandingOrder();
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

            if (currPosition != oldOrder.Count - 1 && oldOrder[currPosition + 1].Weighted == 0)
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

                int oldCnt = _landed.Count;
                int newCnt;
                do
                {
                    ClearCurrentOrder();
                    SetNewOrder(order);
                    // List<LandingData> tmpOrder = Db.GetLandingOrder();
                    List<LandingData> tmpOrder = await GetLandingOrder();
                    newCnt = tmpOrder.Count;
                } while (oldCnt != newCnt);

                // Получить новую очередь плавок
                // Если количество плавок в новой очереди не равно количеству плавок в заданной очереди
                // Очистить текущую очередь
                // Заполнить новую очередь
            }
            else
            {
                _logger.Error($"Плавка [{uid}] находится первой в очереди, некуда опускать");
            }
        }

        private void NextLabelNumber(int uid)
        {
            foreach (LandingData item in _landed)
            {
                if (item.Weighted > 0)
                {
                    _logger.Info($"Установлен номер бунта {CoilData.CoilNumber} для плавки [ID={item.LandingId}, №{item.MeltNumber}]");
                }
            }
        }

        /// <summary>
        /// Очистить текущую очередь на посаде печи
        /// </summary>
        private async void ClearCurrentOrder()
        {
            // List<LandingData> order = Db.GetLandingOrder();
            List<LandingData> order;
            try
            {
                order = await GetLandingOrder();
            }
            catch (Exception ex)
            {
                order = new List<LandingData>();
                _logger.Error($"Не удалось получить текущую очередь [{ex.Message}]");
            }
            
            int i = 1;
            foreach (LandingData melt in order)
            {
                if (melt.Weighted == 0)
                {
                    try
                    {
                        int id = Db.Remove(melt.LandingId);
                        _logger.Warn($"Удалена плавка [{id}] при очистке очереди");
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
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
                // _landed = Db.GetLandingOrder();
                _landed = await GetLandingOrder();
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
        private async void SetNewOrder(List<LandingData> order)
        {
            for (int i = order.Count-1; i >= 0; i--)
            {
                if (order[i].Weighted == 0)
                {
                    try
                    {
                        int id = Db.CreateOvenLanding(order[i]);
                        _logger.Warn($"Добавлена плавка [{id}] при заполнении очереди");
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
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
            LandingData orig = new LandingData();
            
            foreach (LandingData item in _landed)
            {
                if (item.LandingId == uid)
                {
                    edit.LandingId = item.LandingId;
                    orig.LandingId = item.LandingId;
                    edit.MeltNumber = item.MeltNumber;
                    orig.MeltNumber = item.MeltNumber;
                    edit.IngotsCount = item.IngotsCount;
                    orig.IngotsCount = item.IngotsCount;
                    edit.IngotLength = item.IngotLength;
                    orig.IngotLength = item.IngotLength;
                    edit.SteelMark = item.SteelMark;
                    orig.SteelMark = item.SteelMark;
                    edit.IngotProfile = item.IngotProfile;
                    orig.IngotProfile = item.IngotProfile;
                    edit.WeightOne = item.WeightOne;
                    orig.WeightOne = item.WeightOne;
                    edit.WeightAll = item.WeightAll;
                    orig.WeightAll = item.WeightAll;
                    edit.Weighted = item.Weighted;
                    orig.Weighted = item.Weighted;
                    edit.ProductCode = item.ProductCode;
                    orig.ProductCode = item.ProductCode;
                    edit.Customer = item.Customer;
                    orig.Customer = item.Customer;
                    edit.Standart = item.Standart;
                    orig.Standart = item.Standart;
                    edit.Diameter = item.Diameter;
                    orig.Diameter = item.Diameter;
                    edit.Shift = item.Shift;
                    orig.Shift = item.Shift;
                    edit.IngotClass = item.IngotClass;
                    orig.IngotClass = item.IngotClass;
                    edit.ProductProfile = item.ProductProfile;
                    orig.ProductProfile = item.ProductProfile;
                    break;
                }
            }
            
            _landingService.SetEditable(orig, edit);
            // object[] quoteArray = { null };
            await JSRuntime.InvokeAsync<string>("openEditor", null);
        }

        private async void Remove(int uid)
        {
            try
            {
                int id = Db.Remove(uid);
                try
                {
                    // _landed = Db.GetLandingOrder();
                    _landed = await GetLandingOrder();
                }
                catch (Exception e)
                {
                    _logger.Error($"Не удалось удалить плавку {uid} [{e.Message}]");
                }

                _logger.Info($"Удалена плавка [{uid}={id}]");
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

        private async void UpdateData(Object source, ElapsedEventArgs e)
        {
            try
            {
                // _landed = Db.GetLandingOrder();
                _landed = await GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }

            _landingService.IngotsCount = DateTime.Now.Millisecond;
            _semaphoreColor = _semaphoreColor == "lightsteelblue" ? "#1861ac" : "lightsteelblue";
        }

        private async void UpdateMessage(object sender, PropertyChangedEventArgs args)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
}