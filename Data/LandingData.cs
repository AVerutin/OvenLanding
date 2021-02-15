using System;
using System.ComponentModel.DataAnnotations;

namespace OvenLanding.Data
{
    public class LandingData
    {
        [Required]
        [StringLength(10, ErrorMessage = "Слишком длинный номер плавки (ограничение в 10 символов).")]
        public int LandingId { get; set; }          // Идентификатор посада
        public DateTime LandingDate { get; set; }   // Время формирования посада
        public string MeltNumber { get; set; }      //  Номер плавки
        public string SteelMark { get; set; }       // Марка стали
        public string IngotProfile { get; set; }    // Сечение заготовки
        public int IngotsCount { get; set; }        // Количество заготовок в плавке
        public int WeightAll { get; set; }          // Теоретический вес всех заготовок
        public int WeightOne { get; set; }          // Теоретический вес одной заготовки
        public int ProductCode { get; set; }        // Код продукции
        public int IngotLength { get; set; }        // Длина заготовки
        public string Standart { get; set; }        // Стандарт
        public string ProductProfile { get; set; }  // Прокатываемый профиль
        public double Diameter { get; set; }        // Диаметр
        public string Customer { get; set; }        // Заказчик
        public string Shift { get; set; }           // Смена
        public string IngotClass { get; set; }      // Класс
        public int Weighted { get; set; }           // Взвешено бунтов (годного)
        public int WeightedIngots { get; set; }     // Взвешено заготовок (перед печью)
        public bool CanBeDeleted { get; set; }      // Признак возможности удаления посада
        public string Specification { get; set; }   // Спецификация
        public int Lot { get; set; }                // Лот
        public int IngotsInOwen { get; set; }       // Количество заготовок на поде печи
        public int IngotsInMill { get; set; }       // Количество заготовок, выданных из печи в стан
        public int IngotsReturned { get; set; }     // Количество возвращенных заготовок
        public int IngotsBroken { get; set; }       // Количество забракованных заготовок
        public int IngotsMilled { get; set; }       // Прокатано заготовок
        

        public LandingData()
        {
            MeltNumber = "";
            LandingId = default;
            LandingDate = DateTime.MaxValue;
            SteelMark = "";
            IngotProfile = "";
            Standart = default;
            IngotsCount = 0;
            WeightAll = 0;
            IngotLength = 0;
            WeightOne = 0;
            ProductCode = 0;
            ProductProfile = default;
            Diameter = default;
            Customer = default;
            Shift = default;
            IngotClass = default;
            Weighted = default;
            WeightedIngots = default;
            CanBeDeleted = false;
            Specification = default;
            Lot = default;
            IngotsInOwen = default;
            IngotsInMill = default;
            IngotsReturned = default;
            IngotsBroken = default;
            IngotsMilled = default;
        }
    }
}
