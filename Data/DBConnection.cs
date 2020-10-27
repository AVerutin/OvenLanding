using System;
using System.Collections.Generic;
using System.Data;
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
                Connection.Open();
                command.ExecuteNonQuery();
                Connection.Close();
                Result = true;
            }
            catch (Exception e)
            {
                logger.Error($"Не удалось записать данные в базу данных: [{e.Message}]");
                if (Connection.FullState == ConnectionState.Open)
                {
                    Connection.Close();
                }
            }

            return Result;
        }

        /// <summary>
        /// Получить список профилей заготовок
        /// </summary>
        /// <returns>Список профилей заготовок</returns>
        public Dictionary<int, string> GetProfiles()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            int key;
            string value;
            
            string query = "select id, profile from public.profiles order by profile";
            Connection.Open();
            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    key = reader.GetInt32(0);
                    value = reader.GetString(1);
                }
                catch (Exception ex)
                {
                    key = 0;
                    value = "";
                    logger.Error($"Ошибка при получении списка профилей [{ex.Message}]");
                }

                result.Add(key, value);
            }

            reader.Close();
            Connection.Close();

            return result;
        }

        /// <summary>
        /// Получить список марок стали
        /// </summary>
        /// <returns>Список марок стали</returns>
        public Dictionary<int, string> GetSteels()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            int key;
            string value;
            
            string query = "select id, steel from public.steels order by steel";
            Connection.Open();
            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    key = reader.GetInt32(0);
                    value = reader.GetString(1);
                }
                catch (Exception ex)
                {
                    key = 0;
                    value = "";
                    logger.Error($"Ошибка при получении списка марок стали [{ex.Message}]");
                }

                result.Add(key, value);
            }

            reader.Close();
            Connection.Close();

            return result;
        }

        /// <summary>
        /// Получить список партий заготовок на посаде печи
        /// </summary>
        /// <returns></returns>
        public List<LandingTable> GetLandingOrder()
        {
            List<LandingTable> result = new List<LandingTable>();

            string query =
                "select l.order_num as order_num, l.plav_number as plav_num, s.steel as steel, p.profile as profile, l.legal_count-l.real_count as total " +
                "from public.oven_landing l join profiles p on l.profile = p.id join steels s on l.steel_mark = s.id " +
                "where l.legal_count-l.real_count>0 order by l.order_num;";
            Connection.Open();
            NpgsqlCommand comm = new NpgsqlCommand(query, Connection);
            NpgsqlDataReader reader = comm.ExecuteReader();
            while (reader.Read())
            {
                LandingTable item = new LandingTable();
                try
                {
                    item.OrderNum = reader.GetInt32(0);
                    item.PlavNum = reader.GetInt32(1);
                    item.Steel = reader.GetString(2);
                    item.Profile = reader.GetString(3);
                    item.Total = reader.GetInt32(4);
                }
                catch (Exception ex)
                {
                    item.OrderNum = 0;
                    item.PlavNum = 0;
                    item.Steel = "<No data>";
                    item.Profile = "<No data>";
                    item.Total = 0;
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
    }
}

