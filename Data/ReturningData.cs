using System;

namespace OvenLanding.Data
{
    public class ReturningData
    {
        public string Melt { get; set; }            // Номер плавки
        public DateTime TimeBegin { get; set; }     // Время вхождения ЕУ в ТУ
        public DateTime TimeEnd { get; set; }       // Время выхода ЕУ из ТУ
        public int IngotNumber { get; set; }        // Номер заготовки
        public int IngotsCount { get; set; }        // Количество заготовок в посаде
        public DateTime TimeCreateLanding { get; set; } // Время создания посада
        public int IngotWeight { get; set; }        // Реальный вес заготовки

        public ReturningData()
        {
            Melt = default;
            TimeBegin = DateTime.MinValue;
            TimeEnd = DateTime.MinValue;
            IngotNumber = default;
            IngotsCount = default;
            TimeCreateLanding = DateTime.MinValue;
            IngotWeight = default;
        }
    }
}