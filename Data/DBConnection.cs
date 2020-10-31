using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using Npgsql;

namespace OvenLanding.Data
{
    public class DBConnection
    {
        private readonly NpgsqlConnection Connection;
        private readonly string ConnectionString;
        private readonly IConfigurationRoot config;
        private readonly Logger logger;

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
            logger = LogManager.GetCurrentClassLogger();
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            
            string host = config.GetSection("DBConnection:Host").Value;
            int port = Int32.Parse(config.GetSection("DBConnection:Port").Value);
            string database = config.GetSection("DBConnection:Database").Value;
            string user = config.GetSection("DBConnection:UserName").Value;
            string password = config.GetSection("DBConnection:Password").Value;

            ConnectionString =
                $"Server={host};Username={user};Database={database};Port={port};Password={password}"; //";SSLMode=Prefer";

            try
            {
                Connection = new NpgsqlConnection(ConnectionString);
            }
            catch (Exception e)
            {
                logger.Error($"Не удалось подключиться к БД [{e.Message}]");
                throw new DataException($"Ошибка при подключении к базе данных: [{e.Message}]");
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
                logger.Error("Ошибка при создании справочника профилей арматуры");
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
                logger.Error("Ошибка при создании справочника профилей арматуры");
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
                logger.Error("Ошибка при создании справочника ГОСТов");
            }
            
            // Создание справочника заказчиков
            query = "create table if not exists public.customers (" +
                    "id serial not null constraint customers_pk primary key, " +
                    "customer varchar not null); " +
                    "comment on table public.customers is 'Справочник ГОСТов'; " +
                    "alter table public.customers owner to mts;";
            
            res = WriteData(query);
            if (!res)
            {
                logger.Error("Ошибка при создании справочника заказчиков");
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
                logger.Error("Ошибка при создании справочника классов");
            }
            
            // Создание таблицы заготовок на посаде печи
            query = "create table if not exists public.oven_landing (" +
                    "id serial not null, " +
                    "plav_number integer not null, " +
                    "order_num integer not null, " +
                    "steel_mark integer not null, " +
                    "profile integer not null, " +
                    "legal_count integer not null, " +
                    "legal_weight double precision not null, " +
                    "real_count integer not null default 0, " +
                    "real_weight double precision not null default 0, " +
                    "length double precision not null, " +
                    "weight double precision not null, " +
                    "date_ts timestamp with time zone default CURRENT_TIMESTAMP not null, " +
                    "constraint oven_landing_pk primary key (id)); " +
                    "comment on table public.oven_landing is 'Таблица заготовок на посаде печи'; " +
                    "alter table public.oven_landing owner to mts;";
        
            res = WriteData(query);
            if (!res)
            {
                logger.Error("Ошибка при создании таблицы [OvenLanding]");
            }

            return res;
        }

        /// <summary>
        /// Записать данные в таблицу БД
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <returns>Результат выполнения операции</returns>
        public bool WriteData(string query)
        {
            bool Result = false;

            try
            {
                NpgsqlCommand command = new NpgsqlCommand(query, Connection);
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }

                command.ExecuteNonQuery();
                Connection.Close();
                Result = true;
            }
            catch (Exception e)
            {
                logger.Error($"Не удалось записать данные в базу данных: [{e.Message}]");
                Debug.WriteLine(e.Message);
                if (Connection.FullState == ConnectionState.Open)
                {
                    Connection.Close();
                }
            }

            return Result;
        }
        
        /// <summary>
        /// Получить список ГОСТов
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public List<string> GetGosts()
        {
            List<string> result = new List<string>();
            string value;
            
            string query = "select id, gost from public.gosts order by gost";
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    value = reader.GetString(1);
                }
                catch (Exception ex)
                {
                    value = "";
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }

                result.Add(value);
            }

            reader.Close();
            Connection.Close();
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
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
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
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }

                result.Add(value);
            }

            reader.Close();
            Connection.Close();
            return result;
        }
        
        /// <summary>
        /// Получить список заказчиков
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetCustomers()
        {
            List<string> result = new List<string>();
            string value;
            
            string query = "select id, customer from public.customers order by customer";
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    value = reader.GetString(1);
                }
                catch (Exception ex)
                {
                    value = "";
                    logger.Error($"Ошибка при получении списка заказчиков [{ex.Message}]");
                }

                result.Add(value);
            }

            reader.Close();
            Connection.Close();
            return result;
        }
        
        /// <summary>
        /// Получить список келассов
        /// </summary>
        /// <returns>Список заказчиков</returns>
        public List<string> GetClasses()
        {
            List<string> result = new List<string>();
            string value;
            
            string query = "select id, class from public.classes order by class";
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    value = reader.GetString(1);
                }
                catch (Exception ex)
                {
                    value = "";
                    logger.Error($"Ошибка при получении списка классов [{ex.Message}]");
                }

                result.Add(value);
            }

            reader.Close();
            Connection.Close();
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
        /// <returns>UID вставленной записи</returns>
        public int CreateOvenLanding(long meltNum, string profile, string steel, int count, double weightAll,
            double weightOne, double lenght)
        {
            string query = string.Format("SELECT public.f_create_posad ({0}, '{1}', '{2}', {3}, {4}, {5}, {6});",
                meltNum, profile, steel, count, weightAll, weightOne, lenght);

            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            int result = -1;
            while (reader.Read())
            {
                try
                {
                    result = reader.GetInt32(1);
                }
                catch (Exception ex)
                {
                    result = -1;
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }
            }

            reader.Close();
            Connection.Close();
            return result;
        }

        public int IncLanding(int uid)
        {
            // Добавить единицу к наряду
            int result = -1;
            string query = string.Format("select * from public.f_add_unit({0})", uid);
            if (Connection.State!=ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    result = reader.GetInt32(0);
                }
                catch (Exception ex)
                {
                    result = -1;
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }
            }

            reader.Close();
            Connection.Close();
            return result;
        }

        public int DecLanding( int uid)
        {
            // Уменьшить количество единиц в наряде
            int result = -1;
            string query = string.Format("select * from public.f_delete_unit({0})", uid);
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    result = reader.GetInt32(0);
                }
                catch (Exception ex)
                {
                    result = -1;
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }
            }

            reader.Close();
            Connection.Close();
            return result;
        }

        /// <summary>
        /// Добавить наряд в очередь посада печи 
        /// </summary>
        /// <param name="data">Данные по наряду</param>
        /// <returns>UID вставленной записи</returns>
        public int CreateOvenLanding(LandingData data)
        {
            // Добавить поле "КодПродукта" в функцию БД
            string query = string.Format(
                "SELECT public.f_create_queue ('{0}', '{1}', '{2}', {3}, {4}, {5}, {6}, '{7}', {8}, '{9}', '{10}', '{11}', {12});",
                data.MeltNumber, data.IngotProfile, data.SteelMark, data.IngotsCount, data.WeightAll, data.WeightOne,
                data.IngotLenght, data.Gost, data.Diameter, data.Customer, data.Shift, data.Class, data.ProductCode);

            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            int result = -1;
            while (reader.Read())
            {
                try
                {
                    result = reader.GetInt32(0);
                }
                catch (Exception ex)
                {
                    result = -1;
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }
            }

            reader.Close();
            Connection.Close();
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
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
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
                    logger.Error($"Ошибка при получении списка марок стали [{ex.Message}]");
                }

                result.Add(value);
            }

            reader.Close();
            Connection.Close();

            return result;
        }

        /// <summary>
        /// Получить список нарядов заготовок на посаде печи
        /// </summary>
        /// <returns>Список нарядов на посад в печь</returns>
        public List<LandingTable> GetLandingOrder()
        {
            List<LandingTable> result = new List<LandingTable>();

            string query = "select * from public.f_get_queue();";
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                LandingTable item = new LandingTable();
                try
                {
                    item.LandingId = reader.GetInt32(0);
                    item.PlavNum = reader.GetString(1);
                    item.Steel = reader.GetString(2);
                    item.Profile = reader.GetString(3);
                    item.IngotsCount = reader.GetInt32(4);
                    item.WeightAll = reader.GetDouble(5);
                    item.WeightOne = reader.GetDouble(6);
                    item.IngotLength = reader.GetDouble(7);
                    item.Gost = reader.GetString(8);
                    item.Diameter = reader.GetInt32(9);
                    item.Customer = reader.GetString(10);
                    item.Shift = reader.GetString(11);
                    item.Class = reader.GetString(12);
                    item.ProductCode = reader.GetInt32(13);
                    item.Placed = reader.GetInt32(14);
                }
                catch (Exception ex)
                {
                    item.LandingId = 0;
                    item.PlavNum = "";
                    item.Steel = "";
                    item.Profile = "";
                    item.IngotsCount = 0;
                    item.WeightAll = 0;
                    item.WeightOne = 0;
                    item.IngotLength = 0;
                    item.Gost = "";
                    item.Diameter = 0;
                    item.Customer = "";
                    item.Shift = "";
                    item.Class = "";
                    item.ProductCode = 0;
                    item.Placed = 0;
                    logger.Error($"Ошибка при получении списка очереди заготовок на посаде печи [{ex.Message}]");
                }

                result.Add(item);
            }

            reader.Close();
            Connection.Close();

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

