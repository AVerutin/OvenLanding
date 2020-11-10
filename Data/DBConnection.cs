using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NLog;
using Npgsql;

namespace OvenLanding.Data
{
    public class DBConnection
    {
        private readonly NpgsqlConnection _connection;
        private readonly Logger _logger;
        private int _exceptionCode;

        /// <summary>
        /// Конструктор создания подключения к базе данных
        /// </summary>
        public DBConnection()
        {
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

            
            // Читаем параметры подключения к СУБД PostgreSQL
            _logger = LogManager.GetCurrentClassLogger();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            string host = config.GetSection("DBConnection:Host").Value;
            int port = int.Parse(config.GetSection("DBConnection:Port").Value);
            string database = config.GetSection("DBConnection:Database").Value;
            string user = config.GetSection("DBConnection:UserName").Value;
            string password = config.GetSection("DBConnection:Password").Value;

            string connectionString =
                $"Server={host};Username={user};Database={database};Port={port};Password={password}"; //";SSLMode=Prefer";

            try
            {
                _connection = new NpgsqlConnection(connectionString);
            }
            catch (Exception e)
            {
                _logger.Error($"Не удалось создать подключение к БД [{e.Message}]");
                throw new DataException($"Не удалось создать подключение к БД [{e.Message}]");
            }
        }

        public bool DbInit()
        {
            // Создание справочника профилей арматуры
            string query = "create table if not exists public.profiles (" +
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
            query = "create table if not exists public.steels (" +
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
            query = "create table if not exists public.gosts (" +
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
            query = "create table if not exists public.customers (" +
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
            query = "create table if not exists public.classes (" +
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
            query = "create table if not exists public.oven_landing (" +
                    "id serial not null, " +
                    "melt_number varchar(15) not null, " +
                    "ingots_count integer not null, " +
                    "ingot_length integer not null, " +
                    "steel_mark varchar(25) not null, " +
                    "ingot_profile varchar(10) not null, " +
                    "ingot_weight integer not null, " +
                    "production_code integer not null, " +
                    "customer varchar(150) not null, " +
                    "standart varchar(150) not null, " +
                    "diameter integer not null, " +
                    "shift varchar(15) not null, " +
                    "class varchar(50) not null, " +
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
                query = "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
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
        
        public bool IsConnected()
        {
            if (_connection == null)
                return false;
            
            bool result;
            switch (_connection.FullState)
            {
                case ConnectionState.Closed:
                case ConnectionState.Broken:
                    result = false;
                    break;
                case ConnectionState.Open:
                case ConnectionState.Connecting:
                case ConnectionState.Open | ConnectionState.Executing:
                case ConnectionState.Open | ConnectionState.Fetching:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
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

                if (oldMelt.Diameter != newMelt.Diameter)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.Diameter, newMelt.Diameter.ToString());
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

                if (oldMelt.IngotsCount != newMelt.IngotsCount)
                {
                    res = ChangeParam(newMelt.LandingId, LandingParam.IngotsCount, newMelt.IngotsCount.ToString());
                    if (!res)
                    {
                        _logger.Error(
                            $"[{_exceptionCode}] => Ошибка при изменении количества заготовок для плавки №{newMelt.LandingId} на [{newMelt.IngotsCount}]");
                    }
                    else
                    {
                        _logger.Info($"Для плавки №{newMelt.LandingId} изменено количество заготовок с [{oldMelt.IngotsCount}] на [{newMelt.IngotsCount}]");
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
            string query = $"call public.p_set_param({melt}, {(int) param}, '{value}');";
            return WriteData(query);
        }

        /// <summary>
        /// Получить идентификатор последней вставленной строки в таблицу
        /// </summary>
        /// <param name="tableName">Имя таблицы</param>
        /// <returns>Идентификатор последней вставленной строки</returns>
        private int GetLastId(string tableName)
        {
            int lastId = 0;
            string query = $"select max(id) from public.{tableName};";
            
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.FieldCount > 0) // Вместо reader.HasRows 
                    {
                        while (reader.Read()) 
                        {
                            try
                            {
                                lastId = reader.GetInt32(0);
                            }
                            catch (Exception ex)
                            {
                                lastId = 0;
                                _logger.Error(
                                    $"Ошибка при получении максимального идентификатора таблицы {tableName} [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }
            }
            
            // if (IsConnected())
            {
                _connection.Close();
            }
            return lastId;
        }

        /// <summary>
        /// Сохранить текущее состояние полей формы ввода
        /// </summary>
        public void SaveState(LandingData state)
        {
            string query;
            int lastId = GetLastId("oven_landing");
            if (lastId == 0)
            {
                query = "insert into public.oven_landing (melt_number, ingots_count, ingot_length, steel_mark, " +
                        "ingot_profile, ingot_weight, production_code, customer, standart, diameter, shift, class) VALUES (" +
                        "'{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, '{7}', '{8}', {9}, '{10}', '{11}');";
            }
            else
            {
                query = "update public.oven_landing set melt_number='{0}', ingots_count={1}, ingot_length={2}, steel_mark='{3}', " +
                        "ingot_profile='{4}', ingot_weight={5}, production_code={6}, customer='{7}', standart='{8}', diameter={9}, " +
                        "shift='{10}', class='{11}' where id={12};";
            }

            query = string.Format(query, state.MeltNumber, state.IngotsCount, state.IngotLength, state.SteelMark,
                state.IngotProfile, state.WeightOne, state.ProductCode, state.Customer, state.Standart, state.Diameter,
                state.Shift, state.IngotClass, lastId);

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
            
            string query = $"select * from public.oven_landing where id = {lastId};";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result.MeltNumber = reader.GetString(1);
                                result.IngotsCount = reader.GetInt32(2);
                                result.IngotLength = reader.GetInt32(3);
                                result.SteelMark = reader.GetString(4);
                                result.IngotProfile = reader.GetString(5);
                                result.WeightOne = reader.GetInt32(6);
                                result.ProductCode = reader.GetInt32(7);
                                result.Customer = reader.GetString(8);
                                result.Standart = reader.GetString(9);
                                result.Diameter = reader.GetInt32(10);
                                result.Shift = reader.GetString(11);
                                result.IngotClass = reader.GetString(12);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Ошибка при получении сохраненного состояния полей формы ввода [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }
            }
            
            // if (IsConnected())
            {
                _connection.Close();
            }
            return result;
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
                NpgsqlCommand command = new NpgsqlCommand(query, _connection);
                if (!IsConnected())
                {
                    _connection.Open();
                }

                command.ExecuteNonQuery();
                // if(IsConnected())
                {
                    _connection.Close();
                }
                result = true;
            }
            catch (Exception e)
            {
                _logger.Error($"Не удалось записать данные в базу данных: [{e.Message}]");
                _exceptionCode = e.HResult;
                Debug.WriteLine(e.Message);
                // if (IsConnected())
                {
                    _connection.Close();
                }
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

            string query = "select id, gost from public.gosts order by gost";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string value;
                            try
                            {
                                value = reader.GetString(1);
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка стандартов [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(value);
                        }
                    }
                }            
            }
            
            // if (IsConnected())
            {
                _connection.Close();
            }
            return result;
        }

        /// <summary>
        /// Получить список профилей заготовок
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetProfiles()
        {
            List<string> result = new List<string>();
            // int key;
            string value;
            
            string query = "select id, profile from public.profiles order by id";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                // key = reader.GetInt32(0);
                                value = reader.GetString(1);
                            }
                            catch (Exception ex)
                            {
                                // key = 0;
                                value = "";
                                _logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(value);
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
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

            string query = "select id, customer from public.customers order by customer";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string value;
                            try
                            {
                                value = reader.GetString(1);
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка заказчиков [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(value);
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
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

            string query = "select id, class from public.classes order by class";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string value;
                            try
                            {
                                value = reader.GetString(1);
                            }
                            catch (Exception ex)
                            {
                                value = "";
                                _logger.Error($"Ошибка при получении списка классов [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(value);
                        }
                    }
                }
            }

            // if (IsConnected())
            {
                _connection.Close();
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
            string query =
                $"SELECT public.f_create_posad ({meltNum}, '{profile}', '{steel}', {count}, {weightAll}, {weightOne}, {lenght});";

            if (!IsConnected())
            {
                _connection.Open();
            }

            int result = -1;
            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
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
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
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
            // Увеличить количество заготовок в плавке
            int result = -1;
            string query = $"select * from public.f_add_unit({uid})";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = reader.GetInt32(0);
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при увеличении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
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
            // Уменьшить количество заготовок в плавке
            int result = -1;
            string query = $"select * from public.f_delete_unit({uid})";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = reader.GetInt32(0);
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при уменьшении количества заготовок в плавке ({uid}) => [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }
            }

            // if (IsConnected())
            {
                _connection.Close();
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
            // Удалить плавку из очереди
            int result = -1;
            string query = $"select * from public.f_delete_from_queue({uid})";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = reader.GetInt32(0);
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error($"Ошибка при удалении плавки ({uid}) [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }            
            }

            //if (IsConnected())
            {
                _connection.Close();
            }

            return result;
        }

        public void Close()
        {
            if(IsConnected())
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// Добавить наряд в очередь посада печи 
        /// </summary>
        /// <param name="data">Данные по наряду</param>
        /// <returns>UID вставленной записи</returns>
        public int CreateOvenLanding(LandingData data)
        {
            // Добавить поле "КодПродукта" в функцию БД
            string query =
                $"SELECT public.f_create_queue ('{data.MeltNumber}', '{data.IngotProfile}', '{data.SteelMark}', " +
                $"{data.IngotsCount}, {data.WeightAll}, {data.WeightOne}, {data.IngotLength}, '{data.Standart}', " +
                $"{data.Diameter}, '{data.Customer}', '{data.Shift}', '{data.IngotClass}', {data.ProductCode});";

            if (!IsConnected())
            {
                _connection.Open();
            }

            int result = -1;
            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = reader.GetInt32(0);
                            }
                            catch (Exception ex)
                            {
                                result = -1;
                                _logger.Error(
                                    $"Ошибка при добавлении плавки №({data.MeltNumber}) в очередь [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
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
            // int key;
            string value;
            
            string query = "select id, steel from public.steels order by steel";
            if (!IsConnected())
            {
                _connection.Open();
            }

            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                // key = reader.GetInt32(0);
                                value = reader.GetString(1);
                            }
                            catch (Exception ex)
                            {
                                // key = 0;
                                value = "";
                                _logger.Error($"Ошибка при получении списка марок стали [{ex.Message}]");
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(value);
                        }
                    }
                }            
            }

            // if (IsConnected())
            {
                _connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Получить список нарядов заготовок на посаде печи
        /// </summary>
        /// <returns>Список нарядов на посад в печь</returns>
        public List<LandingData> GetLandingOrder()
        {
            if (_connection.State == ConnectionState.Executing)
            {
                _logger.Warn("Выполняется еще предыдущий запрс");
                return new List<LandingData>();
            }

            if (_connection.State == ConnectionState.Fetching)
            {
                _logger.Warn("Производится выборка из предыдущего запроса");
                return new List<LandingData>();
            }
            
            List<LandingData> result = new List<LandingData>();
            string query = "select * from public.f_get_queue();";
            if (!IsConnected())
            {
                _connection.Open();
            }
            
            using (NpgsqlCommand comm = new NpgsqlCommand(query, _connection))
            {
                using (NpgsqlDataReader reader = comm.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            LandingData item = new LandingData();
                            try
                            {
                                item.LandingId = reader.GetInt32(0);
                                item.MeltNumber = reader.GetString(1);
                                item.SteelMark = reader.GetString(2);
                                item.IngotProfile = reader.GetString(3);
                                item.IngotsCount = reader.GetInt32(4);
                                item.WeightAll = reader.GetInt32(5);
                                item.WeightOne = reader.GetInt32(6);
                                item.IngotLength = reader.GetInt32(7);
                                item.Standart = reader.GetString(8);
                                item.Diameter = reader.GetInt32(9);
                                item.Customer = reader.GetString(10);
                                item.Shift = reader.GetString(11);
                                item.IngotClass = reader.GetString(12);
                                item.ProductCode = reader.GetInt32(13);
                                item.Weighted = reader.GetInt32(14);
                            }
                            catch (Exception ex)
                            {
                                item.LandingId = 0;
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
                                // if (IsConnected())
                                {
                                    _connection.Close();
                                }
                            }

                            result.Add(item);
                        }
                    }
                }            
            }
            
            // if (IsConnected())
            {
                _connection.Close();
            }

            return result;
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
                string query = string.Format("insert into public.profiles (profile) values ('{0}');", profileName);
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
                string query = string.Format("insert into public.steels (steel) values ('{0}');", steelName);
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
                string query = string.Format("insert into public.gosts (gost) values ('{0}');", gostName);
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить заказчика
        /// </summary>
        /// <param name="steelName">Заказчик</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddCustomer(string customerName)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(customerName))
            {
                string query = string.Format("insert into public.customers (customer) values ('{0}');", customerName);
                res = WriteData(query);
            }

            return res;
        }
        
        /// <summary>
        /// Добавить класс
        /// </summary>
        /// <param name="steelName">Класс</param>
        /// <returns>Результат выполнения операции</returns>
        public bool AddClass(string className)
        {
            bool res = false;
            if (!string.IsNullOrEmpty(className))
            {
                string query = string.Format("insert into public.classes (class) values ('{0}');", className);
                res = WriteData(query);
            }

            return res;
        }
    }
}

