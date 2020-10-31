namespace OvenLanding.Data
{
    public class LandingTable
    {
        public int LandingId { get; set; }      // идентификатор посада ССМ
        public string PlavNum { get; set; }        // плавка
        public string Steel { get; set; }       // марка стали
        public string Profile { get; set; }     // сечение заготовки
        public int IngotsCount { get; set; }    // количество заготовок
        public double WeightAll { get; set; }   // вес всех заготовок теор
        public double WeightOne { get; set; }   // вес одной заготовки теор
        public double IngotLength { get; set; } // длина заготовки
        public string Gost { get; set; }        // ГОСТ
        public int Diameter { get; set; }       // Диаметр
        public string Customer { get; set; }    // Заказчик
        public string Shift { get; set; }       // Смена
        public string Class { get; set; }       // Класс
        public int Placed { get; set; }        // Количество ЕУ, которым присвоен вес и распечатана бирка
        public int ProductCode { get; set; }   // Код продукции

    }
}
