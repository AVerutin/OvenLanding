using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using System.Threading.Tasks;
using OvenLanding.Data;
using System.Timers;

namespace OvenLanding.Pages
{
    public partial class Start : IDisposable
    {
        private Logger _logger;
        private static readonly DBConnection Db = new DBConnection();
        private static List<LandingData> _landed = new List<LandingData>();
        private List<LandingData> _beforeFurnace = new List<LandingData>();
        private List<LandingData> _inFurnace = new List<LandingData>();
        private List<LandingData> _inReturn = new List<LandingData>();
        private List<LandingData> _inMill = new List<LandingData>();
        
        private List<AreasData> _furnace = new List<AreasData>();
        private List<AreasData> _returning = new List<AreasData>();
        private List<AreasData> _mill = new List<AreasData>();
        private List<AreasData> _pallets = new List<AreasData>();
        private List<AreasData> _hooks = new List<AreasData>();
        
        private static readonly CoilData CoilData = new CoilData();
        private Timer _timer;
        
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

        private void Initialize()
        {
            // Получить список ЕУ на весах перед печью
            _beforeFurnace = GetMeltsBeforeFurnace();
            _inFurnace = GetMeltsInArea(Areas.Furnace);
            _inReturn = GetMeltsInArea(Areas.Returning);
            _inMill = GetMeltsInArea(Areas.Mill);
            
            // _furnace = Db.GetIngotsByArea(Areas.Furnace);
            // _returning = Db.GetIngotsByArea(Areas.Returning);
            // _mill = Db.GetIngotsByArea(Areas.Mill);
            // _pallets = Db.GetIngotsByArea(Areas.Shifter);
            // _hooks = Db.GetIngotsByArea(Areas.Drag);
            
            try
            {
                _landed = Db.GetLandingOrder();
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить очередь на посаде [{ex.Message}]");
            }

            StateHasChanged();
            // SetTimer(3);
        }

        /// <summary>
        /// Получить список плавок у весов перед печью
        /// </summary>
        /// <returns>Список плавок</returns>
        private List<LandingData> GetMeltsBeforeFurnace()
        {
            // Получить очередь перед весами 
            // Получить плавку на весах, определить дату ее постановки в очередь
            // Вывести список плавок, дата постановки которых больше.
            
            // Этап 1 - Получить плавку на весах
            // Этап 2 - Получить время постановки плавки на весах
            // Этап 3 - Получить список плавок, время поставноки в очередь которых больше
            // select * from public.f_get_queue() where c_melt='8200304' and c_diameter=5.5; -- Время постановки плавки на весах в очередь
            // select * from public.f_get_queue() where c_date_reg>'29-12-2020 06:36:21'; -- Список плавок выше то, которая взвешивается

            List<LandingData> result = new List<LandingData>();
            List<AreasData> beforeFurnace = Db.GetIngotsByArea(Areas.BeforeFurnace);
            _landed = Db.GetLandingOrder();

            foreach (LandingData melt in _landed)
            {
                if (melt.LandingDate > beforeFurnace[0].LandingDate)
                {
                    result.Add(melt);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список плавок на участке
        /// </summary>
        /// <param name="area"></param>
        /// <returns>Список плавок</returns>
        private List<LandingData> GetMeltsInArea(Areas area)
        {
            List<LandingData> result = new List<LandingData>();
            List<AreasData> inArea = Db.GetMeltsByArea(area);
            _landed = Db.GetLandingOrder();

            foreach (LandingData melt in _landed)
            {
                foreach (var ml in inArea)
                {
                    if(melt.LandingId == ml.LandingId)
                        result.Add(melt);
                }
            }

            return result;
        }

        /// <summary>
        /// Получмить список плавок в печи
        /// </summary>
        /// <returns>Список плавок</returns>
        private List<LandingData> GetMeltsInFurnace()
        {
            List<LandingData> result = new List<LandingData>();
            List<AreasData> inFurnace = Db.GetMeltsByArea(Areas.Furnace);
            _landed = Db.GetLandingOrder();

            foreach (LandingData melt in _landed)
            {
                foreach (var ml in inFurnace)
                {
                    if(melt.LandingId == ml.LandingId)
                        result.Add(melt);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить список плавок, возвращенных из печи
        /// </summary>
        /// <returns></returns>
        private List<LandingData> GetMeltsInReturn()
        {
            List<LandingData> result = new List<LandingData>();
            List<AreasData> inFurnace = Db.GetMeltsByArea(Areas.Returning);
            _landed = Db.GetLandingOrder();

            foreach (LandingData melt in _landed)
            {
                foreach (var ml in inFurnace)
                {
                    if(melt.LandingId == ml.LandingId)
                        result.Add(melt);
                }
            }

            return result;
        }

        /// <summary>
        /// Увеличить количество заготовок в плавке
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        private void IncLanding(int uid)
        {
            try
            {
                Db.IncLanding(uid);
                _landed = Db.GetLandingOrder();
                _logger.Info($"Добавлена заготовка в плавку [{uid}]");
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось добавить заготовку в плавку [{uid}] => {ex.Message}");
            }
            StateHasChanged();
        }

        /// <summary>
        /// Уменьшить количество заготовок в плавке
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        private void DecLanding(int uid)
        {
            try
            {
                Db.DecLanding(uid);
                _landed = Db.GetLandingOrder();
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
                oldOrder = Db.GetLandingOrder();
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
                    List<LandingData> tmpOrder = Db.GetLandingOrder();
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
                oldOrder = Db.GetLandingOrder();
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
                    List<LandingData> tmpOrder = Db.GetLandingOrder();
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
        private void ClearCurrentOrder()
        {
            List<LandingData> order = Db.GetLandingOrder();
            int i = 1;
            foreach (LandingData melt in order)
            {
                if (melt.Weighted == 0)
                {
                    try
                    {
                        int id = Db.Remove(melt.LandingId);
                        _logger.Warn($"Удалена плавка [{id}] при очистке очереди");
                        Task.Delay(TimeSpan.FromMilliseconds(500));
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
                _landed = Db.GetLandingOrder();
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
                        int id = Db.CreateOvenLanding(order[i]);
                        _logger.Warn($"Добавлена плавка [{id}] при заполнении очереди");
                        Task.Delay(TimeSpan.FromMilliseconds(500));
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

        private void Remove(int uid)
        {
            try
            {
                int id = Db.Remove(uid);
                try
                {
                    _landed = Db.GetLandingOrder();
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

        private void UpdateData(Object source, ElapsedEventArgs e)
        {
            try
            {
                _landed = Db.GetLandingOrder();
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