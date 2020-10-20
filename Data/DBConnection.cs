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
        /// <param name="options">Параметры подключения к базе данных</param>
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
            string query = "create table if not exists public.oven_landing (" +
                           "id  serial not null, " +
                           "plav_number integer not null, " +
                           "steel_mark varchar(15) not null, " +
                           "profile varchar(15) not null, " +
                           "legal_count integer not null, " +
                           "legal_weight double precision not null, " +
                           "real_count integer, " +
                           "real_weight double precision, " +
                           "length double precision not null, " +
                           "weight double precision not null, " +
                           "date_ts timestamptz default CURRENT_TIMESTAMP not null, " +
                           "constraint oven_landing_pk primary key (id) ); " +
                           "alter table public.oven_landing owner to mts;";
        
            bool res = WriteData(query);
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
    }
}