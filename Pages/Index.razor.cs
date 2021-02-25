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
        private bool _movingButtonsState = true;
        
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
                _landed = GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }
            
            _setLoading(false);
            StateHasChanged();
            SetTimer(15);
        }

        private void IncLanding(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    break;
                }
            }
            
            _logger.Info($"===== Начато добавление ЕУ к плавке [{uid}] №{meltNo} =====");
            try
            {
                Db.IncLanding(uid);
                _landed = GetLandingOrder();
                int newCnt = 0;
                foreach (LandingData melt in _landed)
                {
                    if (melt.LandingId == uid)
                    {
                        newCnt = melt.IngotsCount;
                        break;
                    }
                }

                _logger.Info($"Добавлена заготовка в плавку [{uid}] №{meltNo}. Количество заготвок: [{oldCnt}] => [{newCnt}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось добавить заготовку в плавку [{uid}] №{meltNo} => {ex.Message}");
            }
            
            StateHasChanged();
            _logger.Info($"===== Завершено добавление ЕУ к плавке [{uid}] №{meltNo} =====");
        }

        private void DecLanding(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    break;
                }
            }
            
            _logger.Info($"===== Начато удаление ЕУ из плавки [{uid}] №{meltNo} =====");
            try
            {
                Db.DecLanding(uid);
                _landed = GetLandingOrder();
                int newCnt = 0;
                foreach (LandingData melt in _landed)
                {
                    if (melt.LandingId == uid)
                    {
                        newCnt = melt.IngotsCount;
                        break;
                    }
                }
                
                _logger.Info($"Удалена заготовка из плавки [{uid}] №{meltNo}. Количество заготовок: [{oldCnt}] => [{newCnt}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось удалить заготовку из плавки [{uid}] №{meltNo} => {ex.Message}");
            }

            StateHasChanged();
            _logger.Info($"===== Завершено удаление ЕУ из плавки [{uid}] №{meltNo} =====");
        }

        /// <summary>
        /// Перемещение текущей плавки вверх (дальше от печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private async void MoveUp(int uid)
        {
            // Проходим по списку и ищем плавку с требуемым номером
            // Если количество взвешенных заготовок равно нулю
            // Если найденная плавка не первая в списке, и количество взвешенных заготовок у предыдущей плавки равно 0
            // Меняем местами выбранную плавку и предыдущую в списке
            
            _movingButtonsState = false;
            string meltNo = "";
            _setLoading(true);
            bool found = false;
            bool ordered = false;
            List<LandingData> order = new List<LandingData>();
            await Task.Delay(100);
            StateHasChanged();

            // 1. Получаем текущий список плавок
            foreach (LandingData melt in _landed)
            {
                order.Add(melt);
            }

            // 2. Ищем выбранную плавку в очереди
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].LandingId == uid)
                {
                    // Нашли плавку
                    meltNo = order[i].MeltNumber;
                    _logger.Info($"===== Начало перемещения вверх по очереди для плавки [{uid}] №{meltNo} =====");
                    found = true;
                    if (i > 0)
                    {
                        // Это не самая верхняя плавка
                        if (order[i].WeightedIngots == 0)
                        {
                            // Нет взвешенных заготовок
                            if (order[i - 1].WeightedIngots == 0)
                            {
                                // Предыдущая плавка не имеет взвешенных заготовок
                                LandingData tmp = order[i];
                                order[i] = order[i - 1];
                                order[i - 1] = tmp;
                                ordered = true;
                                break;
                            }
                            else
                            {
                                _logger.Error(
                                    $"Предыдущая плавка [{order[i - 1].LandingId}] №{order[i - 1].MeltNumber} имеет взвешенные заготовки! Нельзя поднять вверх!");
                            }
                        }
                        else
                        {
                            _logger.Error(
                                $"Плавка [{uid}] №{order[i].MeltNumber} имеет взвешенные заготовки, нельзя поднять вверх!");
                        }
                    }
                    else
                    {
                        _logger.Error($"Плавка [{uid}] №{order[i].MeltNumber} самая верхняя, нельзя поднять вверх!");
                    }
                }
            }

            if (!found)
                _logger.Error($"Плавка с идентификатором {uid} не найдена в очереди!");
            
            if(ordered)
            {
                int newCnt;
                int oldCnt = order.Count;

                // Пока количество удаленных плавок не будет равно количеству добавленных
                do
                {
                    // Очистить текущую очередь
                    _logger.Info($"Плавка [{uid}] => Начата очистка текущей очереди");
                    ClearCurrentOrder();
                    _logger.Info($"Плавка [{uid}] => Завершена очистка текущей очереди");

                    // Заполнить новую очередь
                    _logger.Info($"Плавка [{uid}] => Начато заполнение нового порядка очереди");
                    SetNewOrder(order);
                    _logger.Info($"Плавка [{uid}] => Завершено заполнение нового порядка очереди");
                    newCnt = _landed.Count;
                } while (oldCnt != newCnt);
            }

            _movingButtonsState = true;
            _setLoading(false);
            _logger.Info($"===== Завершение перемещения вверх по очереди для плавки [{uid}] №{meltNo} =====");
            StateHasChanged();
        }

        /// <summary>
        /// Получить список плавок в очереди
        /// </summary>
        /// <returns></returns>
        private List<LandingData> GetLandingOrder()
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
                
                Task.Delay(500);
            }
            
            return result;
        }

        /// <summary>
        /// Перемещение текущей плавки вниз (ближе к печи)
        /// </summary>
        /// <param name="uid">Идентификатор перемещаемой плавки</param>
        private async void MoveDown(int uid)
        {
            // Проходим по списку и ищем плавку с требуемым номером
            // Если количество взвешенных заготовок равно нулю
            // Если найденная плавка не последняя в списке, и количество взвешенных заготовок у следующей плавки равно 0
            // Меняем местами выбранную плавку и следующую в списке
            
            _movingButtonsState = false;
            _setLoading(true);
            bool found = false;
            bool ordered = false;
            string meltNo = "";
            List<LandingData> order = new List<LandingData>();
            await Task.Delay(100);
            StateHasChanged();

            // 1. Получаем текущий список плавок
            foreach (LandingData melt in _landed)
            {
                order.Add(melt);
            }

            // 2. Ищем выбранную плавку в очереди
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].LandingId == uid)
                {
                    // Нашли плавку
                    meltNo = order[i].MeltNumber;
                    _logger.Info($"===== Начало перемещения вниз по очереди для плавки [{uid}] №{meltNo} =====");
                    found = true;
                    if (i < order.Count - 1)
                    {
                        // Это не самая нижняя плавка
                        if (order[i].WeightedIngots == 0)
                        {
                            // Нет взвешенных заготовок
                            if (order[i + 1].WeightedIngots == 0)
                            {
                                // Следующая плавка не имеет взвешенных заготовок
                                LandingData tmp = order[i];
                                order[i] = order[i + 1];
                                order[i + 1] = tmp;
                                ordered = true;
                                break;
                            }
                            else
                            {
                                _logger.Error(
                                    $"Следующая плавка [{order[i + 1].LandingId}] №{order[i + 1].MeltNumber} имеет взвешенные заготовки! Нельзя опустить вниз!");
                            }
                        }
                        else
                        {
                            _logger.Error(
                                $"Плавка [{uid}] №{order[i].MeltNumber} имеет взвешенные заготовки, нельзя опустить вниз!");
                        }
                    }
                    else
                    {
                        _logger.Error($"Плавка [{uid}] №{order[i].MeltNumber} самая нижняя, нельзя опустить вниз!");
                    }
                }
            }

            if (!found)
                _logger.Error($"Плавка с идентификатором {uid} не найдена в очереди!");
            
            if(ordered)
            {
                int newCnt;
                int oldCnt = order.Count;

                // Пока количество удаленных плавок не будет равно количеству добавленных
                do
                {
                    // Очистить текущую очередь
                    _logger.Info($"Плавка [{uid}] => Начата очистка текущей очереди");
                    ClearCurrentOrder();
                    _logger.Info($"Плавка [{uid}] => Завершена очистка текущей очереди");

                    // Заполнить новую очередь
                    _logger.Info($"Плавка [{uid}] => Начато заполнение нового порядка очереди");
                    SetNewOrder(order);
                    _logger.Info($"Плавка [{uid}] => Завершено заполнение нового порядка очереди");
                    newCnt = _landed.Count;
                } while (oldCnt != newCnt);
            }

            _movingButtonsState = true;
            _setLoading(false);
            _logger.Info($"===== Завершение перемещения вниз по очереди для плавки [{uid}] №{meltNo} =====");
            StateHasChanged();
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
        private void ClearCurrentOrder()
        {
            List<LandingData> order = _landed;
            int i = 1;
            
            foreach (LandingData melt in order)
            {
                if (melt.WeightedIngots == 0)
                {
                    try
                    {
                        _logger.Info($"Начато удаление плавки [{melt.LandingId}] №{melt.MeltNumber} при очистке очереди");
                        int id = Db.Remove(melt.LandingId);
                        _logger.Warn($"Удалена плавка [{id}] №{melt.MeltNumber} при очистке очереди");
                        Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[{i}] Не удалось удалить плавку [{melt.LandingId}] => {ex.Message}");
                    }
                }

                i++;
            }
            
            // Обновление списка плавок после очистки
            try
            {
                _landed = GetLandingOrder();
            }
            catch (Exception ex)
            {
                _landed = new List<LandingData>();
                _logger.Error($"Не удалось получить текущую очередь [{ex.Message}]");
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
                if (order[i].WeightedIngots == 0)
                {
                    try
                    {
                        _logger.Info(
                            $"Начало добавления плавки [{order[i].LandingId}] №{order[i].MeltNumber} при заполнении очереди");
                        int id = Db.CreateOvenLanding(order[i]);
                        _logger.Warn($"Добавлена плавка [{id}] №{order[i].MeltNumber} при заполнении очереди");
                        Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Не удалось добавить плавку {order[i].LandingId} в очередь [{ex.Message}]");
                    }
                }
            }
            
            // Обновление списка плавок после очистки
            try
            {
                _landed = GetLandingOrder();
            }
            catch (Exception ex)
            {
                _landed = new List<LandingData>();
                _logger.Error($"Не удалось получить текущую очередь [{ex.Message}]");
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

        private void Remove(int uid)
        {
            string meltNo = "";
            int oldCnt = 0;
            foreach (LandingData melt in _landed)
            {
                if (melt.LandingId == uid)
                {
                    meltNo = melt.MeltNumber;
                    oldCnt = melt.IngotsCount;
                    break;
                }
            }
            
            _logger.Info($"===== Начато удаление плавки [{uid}] №{meltNo}, содержащей {oldCnt} заготовок из очереди =====");
            try
            {
                int id = Db.Remove(uid);
                try
                {
                    _landed = GetLandingOrder();
                }
                catch (Exception e)
                {
                    _logger.Error($"Не удалось удалить плавку [{uid}] №{meltNo}, содержащую {oldCnt} заготовок из очереди [{e.Message}]");
                }

                _logger.Info($"Удалена из очереди плавка [{uid}] №{meltNo}, содержащая {oldCnt} заготовок");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось удалить плавку [{uid}] №{meltNo}, содержащую {oldCnt} заготовок => {ex.Message}");
            }

            StateHasChanged();
            _logger.Info($"===== Завершено удаление плавки [{uid}] №{meltNo}, содержащей {oldCnt} заготовок из очереди =====");
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
                // _landed = Db.GetLandingOrder();
                _landed = GetLandingOrder();
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