using System;

namespace OvenLanding.Data
{
    /// <summary>
    /// Список ЕУ по ТУ
    /// </summary>
    public class IngotsOnTU
    {
        public int NodeId { get; set; }         // 0 - Идентификатор ТУ
        public string NodeName { get; set; }    // 1 - Наименование ТУ
        public int IngotId { get; set; }        // 2 - Идентификатор ЕУ
        public DateTime TimeBegin { get; set; } // 3 - Время вхождения в ЕУ
        public DateTime TimeEnd { get; set; }   // 4 - Время выхода из ЕУ
        public int BilletWeight { get; set; }   // 5 - Вес заготовки
        public int Position { get; set; }       // 6 - Позиция заготовки в очереди
        public string MeltNumber { get; set; }  // 7 - Номер плавки
        public int CoilNumber { get; set; }     // 8 - Номер бунта
        public int CoilWeight { get; set; }     // 9 - Вес бунта
        public string Section { get; set; }     // 10 - Сечение заготовки
        public string SteelMark { get; set; }   // 11 - Марка стали
        public string Profile { get; set; }     // 12 - Прокатываемый профиль
        public double Diameter { get; set; }    // 13 - Диаметр
        public int IngotsCount { get; set; }    // 14 - Количество заготовок
        public int IngotsWeight { get; set; }   // 15 - Суммарный вес заготовок

        public IngotsOnTU()
        {
            NodeId = default;
            NodeName = default;
            IngotId = default;
            TimeBegin = DateTime.MinValue;
            TimeEnd = DateTime.MinValue;
            BilletWeight = default;
            Position = default;
            MeltNumber = default;
            CoilNumber = default;
            CoilWeight = default;
            Section = default;
            SteelMark = default;
            Profile = default;
            Diameter = default;
            IngotsCount = default;
            IngotsWeight = default;
        }
    }
}