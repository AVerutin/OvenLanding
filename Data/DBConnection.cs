using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NLog;
using Npgsql;
using OvenLanding.Annotations;

namespace OvenLanding.Data
{
    public class DBConnection
    {
        private readonly string _connectionString;
        private readonly string _connectionStringTest;
        private readonly Logger _logger;
        private int _exceptionCode;
        private DBQueries _dbQueries;
        private int _timeOutP;
        private int _timeOutT;

        /// <summary>
        /// Конструктор создания подключения к базе данных
        /// </summary>
        public DBConnection()
        {
            // Открытие транзакции и ограничение ее выполнения по времени
            // begin; set session statement_timeout  to '2s'; commit; SELECT pg_sleep(5);

            // Параметры подключения к БД для удаленного компьютера
            // "DBConnection": {
            //     "Host": "10.23.196.52",
            //     "Port": "5432",
            //     "Database": "mtsbase",
            //     "UserName": "mts",
            //     "Password": "dfaf@we jkjcld!",
            //     "sslmode": "Prefer",
            //     "Trust Server Certificate": "true",
            //     "Reconnect": "20000"
            // }

            // Параметры подключения к БД для локального компьютера
            // "DBConnection": {
            //     "Host": "192.168.56.104",
            //     "Port": "5432",
            //     "Database": "mtsbase",
            //     "UserName": "mts",
            //     "Password": "test$ope$_1",
            //     "sslmode": "Prefer",
            //     "Trust Server Certificate": "true",
            //     "Reconnect": "20000"
            // }
            
            // "DBTest": {
            //  "Host": "10.23.196.58",
            //  "Port": "5432",
            //  "Database": "mtsbase",
            //  "UserName": "mts",
            //  "Password": "dfaf@we jkjcld!",
            //  "SslMode": "Prefer",
            //  "Trust Server Certificate": "true",
            //  "Reconnect": "5000"
            // }
            
            // Читаем параметры подключения к СУБД PostgreSQL
            _logger = LogManager.GetCurrentClassLogger();
            _dbQueries = new DBQueries();
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            // Настройки для подключения к базе данных на проде
            string host = config.GetSection("DBConnection:Host").Value;
            int port = int.Parse(config.GetSection("DBConnection:Port").Value);
            string database = config.GetSection("DBConnection:Database").Value;
            string user = config.GetSection("DBConnection:UserName").Value;
            string password = config.GetSection("DBConnection:Password").Value;
            _timeOutP = int.Parse(config.GetSection("DBConnection:Timeout").Value);
            string sslMode = config.GetSection("DBConnection:SslMode").Value;
            string trustServerCert = config.GetSection("DBConnection:TrustServerCertificate").Value;
            
            // Настройки для подклоючения к базе данных на тесте
            string hostT = config.GetSection("DBTest:Host").Value;
            int portT = int.Parse(config.GetSection("DBTest:Port").Value);
            string databaseT = config.GetSection("DBTest:Database").Value;
            string userT = config.GetSection("DBTest:UserName").Value;
            string passwordT = config.GetSection("DBTest:Password").Value;
            _timeOutT = int.Parse(config.GetSection("DBTest:Timeout").Value);
            string sslModeT = config.GetSection("DBTest:SslMode").Value;
            string trustServerCertT = config.GetSection("DBTest:TrustServerCertificate").Value;

            // Строка подключения для базы данных на проде
            _connectionString =
                $"Server={host};Username={user};Database={database};Port={port};Password={password};" +
                $"SSL Mode={sslMode};Trust Server Certificate={trustServerCert}";
            
            // Строка подключения для базы данных на тесте
            _connectionStringTest =
                $"Server={hostT};Username={userT};Database={databaseT};Port={portT};Password={passwordT};" +
                $"SSL Mode={sslModeT};Trust Server Certificate={trustServerCertT}";
        }

        public bool DbInit()
        {
            // Создание справочника профилей арматуры
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.profiles (" +
                           "id serial not null constraint profiles_pk primary key, " +
                           "profile varchar not null); " +
                           "comment on table public.profiles is 'Справочник видов профилей заготовки'; " +
                           "alter table public.profiles owner to mts;";
            bool res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании справочника профилей арматуры");
            }
            
            // Создание справочника марок стали
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.steels (" +
                    "id serial not null constraint steels_pk primary key, " +
                    "steel varchar not null); " +
                    "comment on table public.steels is 'Справочник марок стали'; " +
                    "alter table public.steels owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании справочника профилей арматуры");
            }
            
            // Создание справочника ГОСТов
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.gosts (" +
                    "id serial not null constraint gosts_pk primary key, " +
                    "gost varchar not null); " +
                    "comment on table public.gosts is 'Справочник ГОСТов'; " +
                    "alter table public.gosts owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании справочника стандартов");
            }
            
            // Создание справочника заказчиков
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.customers (" +
                    "id serial not null constraint customers_pk primary key, " +
                    "customer varchar not null); " +
                    "comment on table public.customers is 'Справочник заказчиков'; " +
                    "alter table public.customers owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании справочника заказчиков");
            }
            
            // Создание справочника классов
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.classes (" +
                    "id serial not null constraint classes_pk primary key, " +
                    "class varchar not null); " +
                    "comment on table public.classes is 'Справочник классов'; " +
                    "alter table public.classes owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании справочника классов");
            }
            
            // Создание таблицы заготовок на посаде печи
            query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "create table if not exists public.oven_landing (" +
                    "id serial not null, " +
                    "melt_number varchar(15), " +
                    "ingots_count numeric, " +
                    "ingot_length numeric, " +
                    "steel_mark varchar(25), " +
                    "ingot_profile varchar(10), " +
                    "ingot_weight numeric, " +
                    "production_code numeric, " +
                    "customer varchar(150), " +
                    "standart varchar(150), " +
                    "diameter numeric, " +
                    "shift varchar(15), " +
                    "class varchar(50), " +
                    "specification varchar(50), " +
                    "lot numeric, " +
                    "constraint oven_landing_pk primary key (id)); " +
                    "comment on table public.oven_landing is 'Сохранение данных полей формы ввода'; " +
                    "alter table public.oven_landing owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при создании таблицы [OvenLanding]");
            }
            
            // Проверяем, есть ли в таблице public.oven_landing записи
            //TODO: Если нет записей в таблице, вылетает исключение 
            if (GetLastId("oven_landing") == 0)
            {
                query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
                        "ingot_profile, ingot_weight, production_code, customer, standart, diameter, shift, class) VALUES (" +
                        "'', 0, 0, '', '', 0, 0, '', '', 0, '', '');";
                res = WriteData(query);
                if (!res)
                {
                    _logger.Error("Ошибка при добавлении записи в таблицу [OvenLanding]");
                }
            }
            
            return res;
        }
        
        public bool EditMelt(LandingData oldMelt, LandingData newMelt)
        {
            bool res = false;
            if (newMelt != null && newMelt.LandingId > 0)
            {
                if (oldMelt.SteelMark != newMelt.SteelMark)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.SteelMark, newMelt.SteelMark);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении марки стали для плавки №{newMelt.LandingId} на [{newMelt.SteelMark}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменена марка стали с [{oldMelt.SteelMark}] на [{newMelt.SteelMark}]");
                    }
                }

                if (oldMelt.IngotProfile != newMelt.IngotProfile)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.IngotProfile, newMelt.IngotProfile);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении сечения заготовки для плавки №{newMelt.LandingId} на [{newMelt.IngotProfile}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменено сечения заготовки с [{oldMelt.IngotProfile}] на [{newMelt.IngotProfile}]");
                    }
                }

                if (oldMelt.ProductProfile != newMelt.ProductProfile)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.ProductProfile, newMelt.ProductProfile);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении профиля годной продукции и для плавки №{newMelt.LandingId} на [{newMelt.ProductProfile}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен профиль годной продукции с [{oldMelt.ProductProfile}] на [{newMelt.ProductProfile}]");
                    }
                }

                if (oldMelt.IngotLength != newMelt.IngotLength)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.IngotLength, newMelt.IngotLength.ToString());
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении длины заготовки для плавки №{newMelt.LandingId} на [{newMelt.IngotLength}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменена длины заготовки с [{oldMelt.IngotLength}] на [{newMelt.IngotLength}]");
                    }
                }

                // if (oldMelt.Shift != newMelt.Shift)
                // {
                //     res = ChangeParam(newMelt.LandingId, LandingParam.ShiftNumber, newMelt.Shift);
                //     if (!res)
                //     {
                //         _logger.Error(
                //             $"[{_exceptionCode}] => Ошибка при изменении номера бригады для плавки №{newMelt.LandingId} на [{newMelt.Shift}]");
                //     }
                //     else
                //     {
                //         _logger.Info($"Для плавки №{newMelt.LandingId} изменен номер бригады с [{oldMelt.Shift}] на [{newMelt.Shift}]");
                //     }
                // }

                if (oldMelt.Standart != newMelt.Standart)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Standart, newMelt.Standart);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении стандарта для плавки №{newMelt.LandingId} на [{newMelt.Standart}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен стандарт с [{oldMelt.Standart}] на [{newMelt.Standart}]");
                    }
                }

                string oldDiam = oldMelt.Diameter.ToString("F1").Replace(",", ".");
                string newDiam = newMelt.Diameter.ToString("F1").Replace(",", ".");
                if (oldDiam != newDiam)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Diameter, newDiam);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении диаметра для плавки №{newMelt.LandingId} на [{newMelt.Diameter}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен диаметра с [{oldMelt.Diameter}] на [{newMelt.Diameter}]");
                    }
                }

                if (oldMelt.Customer != newMelt.Customer)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Customer, newMelt.Customer);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении заказчика для плавки №{newMelt.LandingId} на [{newMelt.Customer}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен заказчик с [{oldMelt.Customer}] на [{newMelt.Customer}]");
                    }
                }

                if (oldMelt.Shift != newMelt.Shift)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Shift, newMelt.Shift);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении бригады для плавки №{newMelt.LandingId} на [{newMelt.Shift}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменена бригада с [{oldMelt.Shift}] на [{newMelt.Shift}]");
                    }
                }

                if (oldMelt.IngotClass != newMelt.IngotClass)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Class, newMelt.IngotClass);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении класса для плавки №{newMelt.LandingId} на [{newMelt.IngotClass}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен класс с [{oldMelt.IngotClass}] на [{newMelt.IngotClass}]");
                    }
                }

                if (oldMelt.MeltNumber != newMelt.MeltNumber)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.MeltNumber, newMelt.MeltNumber);
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении номера плавки для плавки №{newMelt.LandingId} на [{newMelt.MeltNumber}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен номер плавки с [{oldMelt.MeltNumber}] на [{newMelt.MeltNumber}]");
                    }
                }

                if (oldMelt.ProductCode != newMelt.ProductCode)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.ProductCode, newMelt.ProductCode.ToString());
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении кода продукции для плавки №{newMelt.LandingId} на [{newMelt.ProductCode}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен код продукции с [{oldMelt.ProductCode}] на [{newMelt.ProductCode}]");
                    }
                }
                
                if(oldMelt.WeightOne!=newMelt.WeightOne)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.WeightOne, newMelt.WeightOne.ToString());
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении веса одной заготовки для плавки №{newMelt.LandingId} на [{newMelt.WeightOne}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен вес одной заготовки с [{oldMelt.WeightOne}] на [{newMelt.WeightOne}]");
                    }

                    int weightAll = newMelt.IngotsCount * newMelt.WeightOne;
                    res = ChangeParam(newMelt.LandingId, LandingParam.WeightAll, weightAll.ToString());
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении веса всех заготовок для плавки №{newMelt.LandingId} на [{weightAll}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменен вес всех заготовок с [{oldMelt.WeightAll}] на [{weightAll}]");
                    }
                }
            }

            return res;
        }

        private bool ChangeParam(int melt, LandingParam param, string value)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"call public.p_set_param({melt}, {(int) param}, '{value}'); ";

            return WriteData(query);
        }

        /// <summary>
        /// Получить идентификатор последней вставленной строки в таблицу
        /// </summary>
        /// <param name="tableName">Имя таблицы</param>
        /// <returns>Идентификатор последней вставленной строки</returns>
        private int GetLastId(string tableName)
        {
            DataTable dataTable = new DataTable();
            int lastId = 0;
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select max(id) from public.{tableName};";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        lastId = int.Parse(dataTable.Rows[0]["max"].ToString() ?? "0");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return lastId;
        }

        /// <summary>
        /// Сохранить текущее состояние полей формы ввода
        /// </summary>
        public void SaveState(LandingData state)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            int lastId = GetLastId("oven_landing");
            if (lastId == 0)
            {
                query += "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
                        "ingot_profile, ingot_weight, production_code, customer, standart, diameter, shift, class, product_profile, specification, lot) VALUES (" +
                        "'{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, '{7}', '{8}', {9}, '{10}', '{11}', '{12}', '{13}', {14});";
            }
            else
            {
                query += "update public.oven_landing set melt_number='{0}', ingots_count={1}, ingot_length={2}, steel_mark='{3}', " +
                        "ingot_profile='{4}', ingot_weight={5}, production_code={6}, customer='{7}', standart='{8}', diameter={9}, " +
                        "shift='{10}', class='{11}', product_profile='{12}', specification='{13}', lot={14} where id={15};";
            }
            
            string diam = state.Diameter.ToString("F1").Replace(",", ".");
            query = string.Format(query, state.MeltNumber, state.IngotsCount, state.IngotLength, state.SteelMark,
                state.IngotProfile, state.WeightOne, state.ProductCode, state.Customer, state.Standart, diam,
                state.Shift, state.IngotClass, state.ProductProfile, state.Specification, state.Lot, lastId);

            bool res = WriteData(query);
            if (!res)
            {
                _logger.Error("Ошибка при сохранении состояния полей формы ввода");
            }
        }

        /// <summary>
        /// Получить сохраненное состояние полей формы ввода
        /// </summary>
        /// <returns></returns>
        public LandingData GetState()
        {
            int lastId = GetLastId("oven_landing");
            LandingData result = new LandingData();
            DataTable dataTable = new DataTable();
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.oven_landing where id = {lastId}; ";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                result.MeltNumber = dataTable.Rows[i][1].ToString();
                                result.IngotsCount = int.Parse(dataTable.Rows[i][2].ToString() ?? "0");
                                result.IngotLength = int.Parse(dataTable.Rows[i][3].ToString() ?? "0");
                                result.SteelMark = dataTable.Rows[i][4].ToString();
                                result.IngotProfile = dataTable.Rows[i][5].ToString();
                                result.WeightOne = int.Parse(dataTable.Rows[i][6].ToString() ?? "0");
                                result.ProductCode = int.Parse(dataTable.Rows[i][7].ToString() ?? "0");
                                result.Customer = dataTable.Rows[i][8].ToString();
                                result.Standart = dataTable.Rows[i][9].ToString();
                                
                                string diam = dataTable.Rows[i][10].ToString() ?? "0";
                                diam = diam.Replace(".", ",");
                                result.Diameter = double.Parse(diam);
                                result.Shift = dataTable.Rows[i][11].ToString();
                                result.IngotClass = dataTable.Rows[i][12].ToString();
                                result.Specification = dataTable.Rows[i][13].ToString();
                                
                                string lot = dataTable.Rows[i][14].ToString() ?? "0";
                                if (string.IsNullOrEmpty(lot))
                                    lot = "0";
                                result.Lot = int.Parse(lot);
                                result.ProductProfile = dataTable.Rows[i][15].ToString();

                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Ошибка при получении сохраненного состояния полей формы ввода [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Открыть новый, или закрыть ранее открытый простой
        /// </summary>
        /// <param name="startTime">Время начала или окончания простоя</param>
        /// <param name="comment">Комментарий</param>
        public void SetDowntime(DateTime? startTime, [CanBeNull] string comment)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "call public.p_set_downtime(";
            
            if (startTime != null)
            {
                query += $"'{startTime:O}'";
            }
            else
            {
                DateTime start = DateTime.Now.AddMinutes(-3);
                query += $"'{start:O}'";
            }

            if (!string.IsNullOrEmpty(comment))
            {
                query += $", '{comment}'";
            }
            else
            {
                query += ", ''";
            }
            
            query += ");";

            bool res = WriteData(query);
            if (!res)
            {
                _logger.Error($"Не удалось открыть простой с датой начала {startTime} и комментарием {comment}");
            }
        }


        /// <summary>
        /// Записать данные в таблицу БД
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <returns>Результат выполнения операции</returns>
        private bool WriteData(string query)
        {
            bool result = false;

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        command.ExecuteNonQuery();
                    connection.Close();
                }

                result = true;
            }
            catch (Exception e)
            {
                _logger.Error($"Не удалось записать данные в базу данных: [{e.Message}]");
                _exceptionCode = e.HResult;
                Debug.WriteLine(e.Message);
            }

            return result;
        }
        
        /// <summary>
        /// Получить список ГОСТов
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetGosts()
        {
            List<string> result = new List<string>();
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout to '{_timeOutP}ms'; ";
            query += "select id, gost from public.gosts order by gost;";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string value;
                            try
                            {
                                value = dataTable.Rows[i][1].ToString();
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка стандартов [{ex.Message}]");
                            }
                            
                            result.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получить список профилей заготовок
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetProfiles()
        {
            DataTable dataTable = new DataTable();
            List<string> result = new List<string>();
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, profile from public.profiles order by id;";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string value;
                            try
                            {
                                value = dataTable.Rows[i][1].ToString();
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                            }
                            
                            result.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }
        
        /// <summary>
        /// Получить список заказчиков
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetCustomers()
        {
            List<string> result = new List<string>();
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, customer from public.customers order by customer;";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string value;
                            try
                            {
                                value = dataTable.Rows[i][1].ToString();
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка заказчиков [{ex.Message}]");
                            }
                            
                            result.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }
        
        /// <summary>
        /// Получить список келассов
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetClasses()
        {
            List<string> result = new List<string>();
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, class from public.classes order by class;";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string value;
                            try
                            {
                                value = dataTable.Rows[i][1].ToString();
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка классов [{ex.Message}]");
                            }
                            
                            result.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получить количество взвешенных заготовок по номеру плавки и диаметру
        /// </summary>
        /// <param name="meltId">Идентификатор плавки</param>
        /// <returns>Количество взвешенных заготовок</returns>
        public WeightedIngotsCount GetWeightedIngotsCount(int meltId)
        {
            WeightedIngotsCount result = new WeightedIngotsCount();
            DataTable dataTable = new DataTable();
            
            string query = $"set session statement_timeout  to '{_timeOutT}ms'; ";
            query +=
                "select rp.unit_id_parent id_posad_test, " +
                "pr.value_s as id_posad_prod, " +
                "pm.value_s melt, " +
                "pc.value_n count_posad, " +
                "count(distinct l.unit_id) count_dt " +
                "from mts.locations l join mts.units_relations r on r.unit_id_child = l.unit_id " +
                "join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 20100 " +
                "join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
                "join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001 " +
                "join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004 " +
                "join mts.passport pr on pr.unit_id = rp.unit_id_parent and pr.param_id=1 " +
                "join mts.passport p on p.unit_id = l.unit_id and p.param_id = 1000 " +
                "where l.node_id = 20100 and l.tm_beg >= now() - interval '1 day' " +
                $"and pr.value_s = '{meltId}' group by rp.unit_id_parent,pr.value_s,pm.value_s,pc.value_n;";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                result.LandingTestId = int.Parse(val);
                                
                                val = dataTable.Rows[i][1].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                result.LandingProdId = int.Parse(val);
                                
                                val = dataTable.Rows[i][2].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                result.Melt = val;
                                
                                val = dataTable.Rows[i][3].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                result.LandingCount = int.Parse(val);
                                
                                val = dataTable.Rows[i][4].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                result.WeightedCount = int.Parse(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось прочитать количество взвешенных заготвок для плавки с идентификатором [{meltId}] - {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"Не удалось получить количество взвешенных заготвок для плавки с идентификатором [{meltId}] - {ex.Message}");
            }

            return result;
        }
        
        /// <summary>
        /// Получить список марок стали
        /// </summary>
        /// <returns>Список марок стали</returns>
        public List<string> GetSteels()
        {
            List<string> result = new List<string>();
            DataTable dataTable = new DataTable();
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "select id, steel from public.steels order by steel";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string value;
                            try
                            {
                                value = dataTable.Rows[i][1].ToString();
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка марок стали [{ex.Message}]");
                            }
                            
                            result.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }


        /// <summary>
        /// Добавить наряд в очередь посада печи 
        /// </summary>
        /// <param name="meltNum">Номер плавки</param>
        /// <param name="profile">Сечение заготовки</param>
        /// <param name="steel">Марка стали</param>
        /// <param name="count">Количество заготовок</param>
        /// <param name="weightAll">Теоретический вес заготовок</param>
        /// <param name="weightOne">Теоретический вес одной заготовки</param>
        /// <param name="lenght">Длина заготовки</param>
        /// <returns>Идентификатор вставленной записи</returns>
        public int CreateOvenLanding(long meltNum, string profile, string steel, int count, double weightAll,
            double weightOne, double lenght)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query +=
                $"SELECT public.f_create_posad ({meltNum}, '{profile}', '{steel}', {count}, {weightAll}, {weightOne}, {lenght});";
            
            int result = -1;

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                using (NpgsqlCommand comm = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    result = reader.GetInt32(1);
                                }
                                catch (Exception ex)
                                {
                                    result = -1;
                                    _logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Увеличить количество заготовок в плавке по идентификатору плавки
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <returns>Идентификатор измененной плавки</returns>
        public int IncLanding(int uid)
        {
            DataTable dataTable = new DataTable();
            int result = -1;
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_add_unit({uid})";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при увеличении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Уменьшить количество заготовок в плавке по идентификатору плавки
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <returns>Идентификаторр измененной плавки</returns>
        public int DecLanding(int uid)
        {
            DataTable dataTable = new DataTable();
            int result = -1;
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_delete_unit({uid})";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при уменьшении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Удалить плавку из очереди
        /// </summary>
        /// <param name="uid">Идентификатор плавки</param>
        /// <returns>Идентификатор удаленной плавки</returns>
        public int Remove(int uid)
        {
            DataTable dataTable = new DataTable();
            int result = -1;
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_delete_from_queue({uid})";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                string res = dataTable.Rows[i][0].ToString();
                                if (string.IsNullOrEmpty(res))
                                {
                                    res = "1";
                                }

                                result = int.Parse(res);
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error($"Ошибка при удалении плавки ({uid}) [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Добавить наряд в очередь посада печи 
        /// </summary>
        /// <param name="data">Данные по наряду</param>
        /// <returns>UID вставленной записи</returns>
        public int CreateOvenLanding(LandingData data)
        {
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            string diam = data.Diameter.ToString("f1").Replace(",", ".");
            query +=
                $"SELECT public.f_create_queue ('{data.MeltNumber}', '{data.IngotProfile}', '{data.SteelMark}', " +
                $"{data.IngotsCount}, {data.WeightAll}, {data.WeightOne}, {data.IngotLength}, '{data.Standart}', " +
                $"{diam}, '{data.Customer}', '{data.Shift}', '{data.IngotClass}', {data.ProductCode}, '{data.ProductProfile}');";

            int result = -1;
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                result = int.Parse(dataTable.Rows[i][0].ToString() ?? "-1");
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при добавлении плавки №({data.MeltNumber}) в очередь [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получить список плавок на участке
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public List<AreasData> GetMeltsByArea(Areas area)
        {
            List<AreasData> result = new List<AreasData>();
            DataTable dataTable = new DataTable();
            string thread = "";
            string ret = "0";
            switch (area)
            {
                case Areas.Returning :
                {
                    thread = "5";
                    ret = "50100";
                    break;
                }
                case Areas.Mill :
                {
                    thread = "5";
                    break;
                }
                case Areas.BeforeFurnace : thread = "2"; break;
                case Areas.Furnace : thread = "3"; break;
                case Areas.Shifter : thread = "11"; break;
                case Areas.Binding : thread = "14"; break;
                case Areas.Scales : thread = "15"; break;
                case Areas.Drag : thread = "17"; break;
            }
            
            string query = $"set session statement_timeout  to '{_timeOutT}ms'; ";
            query += "select c_id_posad, c_melt, c_diam, max(c_date_reg) as c_date_reg\n";
            query += $"from public.f_get_queue_thread({thread}, {ret})\n";
            query += "group by c_melt, c_diam, c_id_posad;";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            AreasData item = new AreasData();

                            try
                            {
                                string val = dataTable.Rows[i][0].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.LandingId = int.Parse(val);

                                val = dataTable.Rows[i][1].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.MeltNumber = val;

                                val = dataTable.Rows[i][2].ToString()?.Trim().Replace(".",",");
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Diameter = double.Parse(val);

                                val = dataTable.Rows[i][3].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.LandingDate = DateTime.Parse(val);
                                
                                result.Add(item);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось прочитать плавок заготовок на участке {area.ToString()} [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить список плавок на участке {area.ToString()} [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        /// Получить список ЕУ на участке
        /// </summary>
        /// <param name="area">Участок цеха</param>
        /// <returns>Список единиц учета</returns>
        public List<AreasData> GetIngotsByArea(Areas area)
        {
            // string thread = "0";
            // string returning = "0";
            string areas = "";
            switch (area)
            {
                case Areas.Returning :
                {
                    // thread = "5";
                    // returning = "50100";
                    areas = "n.thread_id=5 and n.id=50100";
                    break;
                }
                case Areas.Mill :
                {
                    // thread = "5";
                    // returning = "0";
                    areas = "n.thread_id=5 and n.id=0";
                    break;
                }
                case Areas.BeforeFurnace : areas = "n.thread_id=2"; break;
                case Areas.Furnace : areas = "n.thread_id=3"; break;
                case Areas.Shifter : areas = "n.thread_id=11"; break;
                case Areas.Binding : areas = "n.thread_id=14"; break;
                case Areas.Scales : areas = "n.thread_id=15"; break;
                case Areas.Drag : areas = "n.thread_id=17"; break;
            }
            
            List<AreasData> result = new List<AreasData>();
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout  to '{_timeOutT}ms'; ";
            query += "select distinct\n";
            query += "\trp.unit_id_parent id_posad,\n";
            query +=
                "\tfirst_value(to_char(l.tm_beg,'DD-MM-YYYY HH24:MI:SS')) over (partition by l.unit_id order by l.id desc) date_enter,\n";
            query += "\tfirst_value(n.thread_id) over (partition by l.unit_id order by l.id desc) thread,\n";
            query += "\tfirst_value(n.id) over (partition by l.unit_id order by l.id desc) node_id,\n";
            query += "\tfirst_value(n.node_code) over (partition by l.unit_id order by l.id desc) node_name,\n";
            query += "\tpm.value_s as melt,\n";
            query += "\tpd.value_s as diam,\n";
            query += "\tt.date_reg date_reg,\n";
            query += "\tt.pos,\n";
            query += "\tpc.value_s as count_posad,\n";
            query += "\tp.value_n as billet_weight\n";
            query += "from mts.locations l\n";
            query += "\tjoin mts.nodes n on n.id = l.node_id\n";
            query += "\tjoin mts.units u on u.id = l.unit_id\n";
            query += "\tleft join mts.units_relations r on r.unit_id_child = u.id\n";
            query += "\tleft join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 20100\n";
            query += "\tleft join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent\n";
            query += "\tleft join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001\n";
            query += "\tleft join mts.passport pd on pd.unit_id = rp.unit_id_parent and pd.param_id=10000010\n";
            query += "\tleft join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004\n";
            query += "\tleft join mts.passport pr on pr.unit_id = rp.unit_id_parent and pr.param_id=1\n";
            query += "\tleft join mts.passport p on p.unit_id = l.unit_id and p.param_id=1000\n";
            query += "\tleft join mts.passport pw on pw.unit_id = t.unit_id and pw.param_id=10000014\n";
            query += $"where l.node_id in (select id from mts.nodes n where {areas})\n";
            query += "\tand l.tm_end is null\n";
            query += "order by 4,5,2 desc;";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            AreasData item = new AreasData();
                            
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.LandingId = int.Parse(val);

                                val = dataTable.Rows[i][1].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.DateEnter = DateTime.Parse(val);
                                
                                val = dataTable.Rows[i][2].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Thread = int.Parse(val);

                                val = dataTable.Rows[i][3].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.NodeId = int.Parse(val);

                                val = dataTable.Rows[i][4].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.NodeName = val;

                                val = dataTable.Rows[i][5].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.MeltNumber = val;
                                
                                val = dataTable.Rows[i][6].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                val = val.Replace(".", ",");
                                item.Diameter = double.Parse(val);
                                
                                val = dataTable.Rows[i][7].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.LandingDate = DateTime.Parse(val);

                                val = dataTable.Rows[i][8].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Position = int.Parse(val);
                                
                                val = dataTable.Rows[i][9].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CountPosad = int.Parse(val);
                                
                                val = dataTable.Rows[i][10].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.BilletWeight = int.Parse(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось прочитать список заготовок на участке {area.ToString()} [{ex.Message}]");
                            }

                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить список заготовок на участке {area.ToString()} [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        /// Получить список возвратов по номеру плавки
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <returns>Список возвратов</returns>
        public List<ReturningData> GetReturns(string melt)
        {
            string query = _dbQueries.GetReturnsByMelt(melt, _timeOutT);
            List<ReturningData> result = _getReturns(query);

            return result;
        }

        /// <summary>
        /// Получить список возвратов за период
        /// </summary>
        /// <param name="begin">Начало периода</param>
        /// <param name="end">Конец периода</param>
        /// <returns>Список возвратов</returns>
        public List<ReturningData> GetReturns(DateTime begin, DateTime end)
        {
            string query = _dbQueries.GetReturnsByPeriod(begin, end, _timeOutT);
            List<ReturningData> result = _getReturns(query);

            return result;
        }

        /// <summary>
        /// Получить список возвратов по готовому запросу
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Список возвратов</returns>
        private List<ReturningData> _getReturns(string query)
        {
            List<ReturningData> result = new List<ReturningData>();
            DataTable dataTable = new DataTable();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            ReturningData item = new ReturningData();
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Melt = val;

                                val = dataTable.Rows[i][1].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.TimeBegin = DateTime.Parse(val);

                                val = dataTable.Rows[i][2].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.TimeEnd = DateTime.Parse(val);

                                val = dataTable.Rows[i][3].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotNumber = int.Parse(val);

                                val = dataTable.Rows[i][4].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotsCount = int.Parse(val);

                                val = dataTable.Rows[i][5].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.TimeCreateLanding = DateTime.Parse(val);

                                val = dataTable.Rows[i][6].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotWeight = int.Parse(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось прочитать список возвратов по готовому запросу [{ex.Message}]");
                            }

                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить список возвратов по готовому запросу [{ex.Message}]");
            }

            return result;
        }


        /// <summary>
        /// Получить список ЕУ и последний ТУ, до которого он дошел по номеру плавки
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <returns>Список ЕУ</returns>
        public List<IngotsOnTU> GetIngotsByTu(string melt)
        {
            List<IngotsOnTU> result = new List<IngotsOnTU>();
            string query = _dbQueries.GetIngotsMyMeltQuery(melt, _timeOutT);
            DataTable dataTable = new DataTable();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionStringTest))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            IngotsOnTU item = new IngotsOnTU();
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.NodeId = int.Parse(val);
                                
                                val = dataTable.Rows[i][1].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.NodeName = val;
                                
                                val = dataTable.Rows[i][2].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotId = int.Parse(val);
                                
                                val = dataTable.Rows[i][3].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.TimeBegin = DateTime.Parse(val);
                                
                                val = dataTable.Rows[i][4].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.TimeEnd = DateTime.Parse(val);
                                
                                val = dataTable.Rows[i][5].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.BilletWeight = int.Parse(val);
                                
                                val = dataTable.Rows[i][6].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Position = int.Parse(val);
                                
                                val = dataTable.Rows[i][7].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.MeltNumber = val;
                                
                                val = dataTable.Rows[i][8].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilNumber = int.Parse(val);
                                
                                val = dataTable.Rows[i][9].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilWeight = int.Parse(val);
                                
                                val = dataTable.Rows[i][10].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0x0";
                                item.Section = val;
                                
                                val = dataTable.Rows[i][11].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.SteelMark = val;
                                
                                val = dataTable.Rows[i][12].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "";
                                item.Profile = val;
                                
                                val = dataTable.Rows[i][13].ToString()?.Trim().Replace(".", ",");
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.Diameter = double.Parse(val);
                                
                                val = dataTable.Rows[i][14].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotsCount = int.Parse(val);
                                
                                val = dataTable.Rows[i][15].ToString()?.Trim();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.IngotsWeight = int.Parse(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось прочитать список заготовок для плавки №{melt} [{ex.Message}]");
                            }
                            
                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"Не удалось получить список заготовок для плавки №{melt} [{ex.Message}]");
            }

            return result;
        }


        /// <summary>
        /// Получить список нарядов заготовок на посаде печи
        /// </summary>
        /// <returns>Список нарядов на посад в печь</returns>
        public List<LandingData> GetLandingOrder(string melt="", double diameter=0.0)
        {
            List<LandingData> result = new List<LandingData>();
            DataTable dataTable = new DataTable();
            
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += string.IsNullOrEmpty(melt)
                ? "select * from public.f_get_queue();"
                : $"select * from public.f_get_queue() where c_melt='{melt}' and c_diameter={diameter.ToString("F1").Replace(",", ".")};";

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            LandingData item = new LandingData();
                            try
                            {
                                item.LandingId = int.Parse(dataTable.Rows[i][0].ToString() ?? "0");
                                item.MeltNumber = dataTable.Rows[i][1].ToString();
                                item.SteelMark = dataTable.Rows[i][2].ToString();
                                item.IngotProfile = dataTable.Rows[i][3].ToString();
                                item.IngotsCount = int.Parse(dataTable.Rows[i][4].ToString() ?? "0");
                                item.WeightAll = int.Parse(dataTable.Rows[i][5].ToString() ?? "0");
                                item.WeightOne = int.Parse(dataTable.Rows[i][6].ToString() ?? "0");
                                item.IngotLength = int.Parse(dataTable.Rows[i][7].ToString() ?? "0");
                                item.Standart = dataTable.Rows[i][8].ToString();

                                string diam = dataTable.Rows[i][9].ToString() ?? "0";
                                diam = diam.Replace(".", ",");
                                item.Diameter = double.Parse(diam);
                                
                                item.Customer = dataTable.Rows[i][10].ToString();
                                item.Shift = dataTable.Rows[i][11].ToString();
                                item.IngotClass = dataTable.Rows[i][12].ToString();
                                item.ProductCode = int.Parse(dataTable.Rows[i][13].ToString() ?? "0");
                                item.ProductProfile = dataTable.Rows[i][14].ToString();
                                item.Weighted = int.Parse(dataTable.Rows[i][15].ToString() ?? "0");
                            }
                            catch (Exception ex)
                            {
                                item.LandingId = 0;
                                item.LandingDate = DateTime.MinValue;
                                item.MeltNumber = "";
                                item.SteelMark = "";
                                item.IngotProfile = "";
                                item.IngotsCount = 0;
                                item.WeightAll = 0;
                                item.WeightOne = 0;
                                item.IngotLength = 0;
                                item.Standart = "";
                                item.Diameter = 0;
                                item.Customer = "";
                                item.Shift = "";
                                item.IngotClass = "";
                                item.ProductCode = 0;
                                item.Weighted = 0;
                                _logger.Error(
                                    $"Ошибка при получении списка очереди заготовок на посаде печи [{ex.Message}]");
                            }
                            
                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка получения данных из БД: {ex.Message}");
            }

            return result;
        }

        public List<CoilData> GetCoilData(bool current=true, bool last=true)
        {
            List<CoilData> result = new List<CoilData>();
            if (current)
            {
                // Получить список бунтов для текущей плавки
                List<LandingData> landed = GetLandingOrder();
                foreach (LandingData item in landed)
                {
                    if (item.Weighted > 0)
                    {
                        result = GetCoilsByMelt(item.MeltNumber, item.Diameter, last);
                    }
                }
            }
            else
            {
                // Получить список бунтов для предыдущей плавки
                Dictionary<string, double> previous = GetPreviousMeltNumber();
                foreach (KeyValuePair<string, double> melt in previous)
                {
                    if (!string.IsNullOrEmpty(melt.Key) && melt.Value > 0)
                    {
                        result = GetCoilsByMelt(melt.Key, melt.Value, last);
                    }
                }
                
            }

            return result;
        }

        private Dictionary<string, double> GetPreviousMeltNumber()
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            DataTable dataTable = new DataTable();

            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += "call public.p_get_previos_melt(null, null);";
            
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            string key = "";
                            double value = 0;
                            
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                key = val;

                                val = dataTable.Rows[i][1].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                val = val.Replace(".", ",");
                                value = double.Parse(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось получить номер и диаметр предыдущей провешеной плавки [{ex.Message}]");
                            }

                            result.Add(key, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить номер и диаметр предыдущей провешеной плавки [{ex.Message}]");
            }

            return result;
        }

        public List<CoilData> GetCoilsByMelt(string melt, double diameter, bool last=true)
        {
            List<CoilData> result = new List<CoilData>();
            DataTable dataTable = new DataTable();
            
            string diam = diameter.ToString("F1").Replace(",",".");
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";

            if (!last)
            {
                query += $"select * from public.f_get_queue_coils('{melt}', {diam});";
            }
            else
            {
                query +=
                    $"select * from public.f_get_queue_coils('{melt}', {diam}) order by c_date_weight desc limit 1;";
            }

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {

                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            CoilData item = new CoilData();
                            try
                            {
                                string val = dataTable.Rows[i][1].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.MeltNumber = val;
                                
                                val = dataTable.Rows[i][9].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = " ";
                                item.ProductionProfile = val;

                                val = dataTable.Rows[i][10].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                val = val.Replace(".", ",");
                                item.Diameter = double.Parse(val);

                                val = dataTable.Rows[i][15].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilUid = int.Parse(val);

                                val = dataTable.Rows[i][16].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilPos = int.Parse(val);

                                val = dataTable.Rows[i][17].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilNumber = int.Parse(val);

                                val = dataTable.Rows[i][18].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.WeightFact = int.Parse(val);

                                val = dataTable.Rows[i][19].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.ShiftNumber = val;

                                val = dataTable.Rows[i][22].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.DateReg = DateTime.Parse(val);

                                val = dataTable.Rows[i][23].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = DateTime.MinValue.ToString("G");
                                item.DateWeight = DateTime.Parse(val);

                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось получить список бунтов для плавки №{melt} с диаметром {diam} [{ex.Message}]");
                            }

                            result.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Не удалось получить данные для плавки №{melt} с диаметром {diam} [{ex.Message}]");
            }

            return result;
        }

        /// <summary>
        /// Вернуть бунт в очередь на взвешивание
        /// </summary>
        /// <param name="coilUid">Идентификатор бунта</param>
        public void ResetCoil(int coilUid)
        {
            string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
            query += $"select * from public.f_return_to_queue({coilUid}); ";
            
            DataTable dataTable = new DataTable();

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    new NpgsqlDataAdapter(new NpgsqlCommand(query, connection)).Fill(dataTable);
                    connection.Close();
                    if (dataTable.Rows.Count > 0)
                    {

                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            CoilData item = new CoilData();
                            try
                            {
                                string val = dataTable.Rows[i][0].ToString();
                                if (string.IsNullOrEmpty(val))
                                    val = "0";
                                item.CoilUid = int.Parse(val);
                                _logger.Info($"Произведен сброс веса бунта [{val}]");
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Не удалось сбросить вес бунта {coilUid} [{ex.Message}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Перевесить бунт с идентификатором {coilUid} [{ex.Message}]");
            }
        }


        /// <summary>
        /// Добавить профиль заготовки
        /// </summary>
        /// <param name="profileName">Профиль заготовки</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddProfile(string profileName)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(profileName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.profiles (profile) values ('{profileName}'); ";
                
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить марку стали
        /// </summary>
        /// <param name="steelName">Марка стали</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddSteel(string steelName)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(steelName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.steels (steel) values ('{steelName}'); ";
                
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить ГОСТ
        /// </summary>
        /// <param name="gostName">ГОСТ</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddGost(string gostName)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(gostName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.gosts (gost) values ('{gostName}'); ";
                
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить заказчика
        /// </summary>
        /// <param name="customerName">Заказчик</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddCustomer(string customerName)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(customerName))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.customers (customer) values ('{customerName}'); ";
                
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить класс
        /// </summary>
        /// <param name="className">Класс</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddClass(string className)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(className))
            {
                string query = $"set session statement_timeout  to '{_timeOutP}ms'; ";
                query += $"insert into public.classes (class) values ('{className}'); ";
                
                res = WriteData(query);
            }

            return res;
        }
    }
}

    // Example 
    // public string getLastUnit()
    // {
    //   string connectionString = string.Format(Program.conf.GetSection("database")["connection_string"]);
    //   DataTable dataTable = new DataTable();
    //   try
    //   {
    //     using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
    //     {
    //       DataSet dataSet = new DataSet();
    //       connection.Open();
    //       new NpgsqlDataAdapter(new NpgsqlCommand("call public.p_first_unit_in_queue(null,null, null, null, null, null, null, null,null);", connection)).Fill(dataTable);
    //       connection.Close();
    //       string str = "" + "p_gost  " + dataTable.Rows[0]["p_gost"].ToString() + " " + Environment.NewLine + "p_melt  " + dataTable.Rows[0]["p_melt"].ToString() + " " + Environment.NewLine + "p_diameter  " + dataTable.Rows[0]["p_diameter"].ToString() + " " + Environment.NewLine + "p_steel_grade  " + dataTable.Rows[0]["p_steel_grade"].ToString() + " " + Environment.NewLine + "p_pos  " + dataTable.Rows[0]["p_pos"].ToString() + " " + Environment.NewLine + "p_customer  " + dataTable.Rows[0]["p_customer"].ToString() + " " + Environment.NewLine + "p_shift  " + dataTable.Rows[0]["p_shift"].ToString() + " " + Environment.NewLine + "p_prod_code  =  " + dataTable.Rows[0]["p_prod_code"].ToString() + " " + Environment.NewLine + "p_class  " + dataTable.Rows[0]["p_class"].ToString() + " " + Environment.NewLine;
    //       Program.logger.Debug("Получили следующие значения от БД " + str);
    //     }
    //   }
    //   catch (Exception ex)
    //   {
    //     Program.logger.Error("Ошибка получения данных из БД " + ex.Message);
    //   }
    //   testController.birkaLabels birkaLabels = new testController.birkaLabels();
    //   if (dataTable.Rows[0]["p_melt"].ToString() == "-1")
    //   {
    //     birkaLabels.HEAT = dataTable.Rows[0]["p_melt"].ToString();
    //     return JsonSerializer.Serialize<testController.birkaLabels>(birkaLabels);
    //   }
    //   birkaLabels.GOST = dataTable.Rows[0]["p_gost"].ToString();
    //   birkaLabels.HEAT = dataTable.Rows[0]["p_melt"].ToString();
    //   birkaLabels.DIAMETER = dataTable.Rows[0]["p_diameter"].ToString();
    //   birkaLabels.STEEL_GRADE = dataTable.Rows[0]["p_steel_grade"].ToString();
    //   int num = int.Parse(dataTable.Rows[0]["p_pos"].ToString()) + 100;
    //   birkaLabels.COIL_NO = num.ToString();
    //   birkaLabels.CUSTOMER = dataTable.Rows[0]["p_customer"].ToString();
    //   birkaLabels.SHIFT = dataTable.Rows[0]["p_shift"].ToString();
    //   birkaLabels.KLASS = dataTable.Rows[0]["p_class"].ToString();
    //   birkaLabels.ProductType = dataTable.Rows[0]["p_prod_code"].ToString();
    //   birkaLabels.COIL_WGT_DATE = DateTime.Now.ToString();
    //   return JsonSerializer.Serialize<testController.birkaLabels>(birkaLabels);
    // }

    // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    // Encoding.GetEncoding("windows-1254");
    // Encoding encoding = Encoding.GetEncoding("windows-1254");
    