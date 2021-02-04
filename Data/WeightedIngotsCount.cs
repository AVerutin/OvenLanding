namespace OvenLanding.Data
{
    public class WeightedIngotsCount
    {
        public int LandingTestId { get; set; } // Идент посада на тесте
        public int LandingProdId { get; set; } // Идент посада на проде
        public string Melt { get; set; }     // Номер плавки
        public int LandingCount { get; set; }  // Количество заготовок в плавке
        public int WeightedCount { get; set; }  // Количество взвешенных заготовок

        public WeightedIngotsCount()
        {
            LandingTestId = default;
            LandingProdId = default;
            Melt = default;
            LandingCount = default;
            WeightedCount = default;
        }
    }
}