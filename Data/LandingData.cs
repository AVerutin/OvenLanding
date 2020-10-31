namespace OvenLanding.Data
{
    public class LandingData
    {
        public string MeltNumber { get; set; }
        public string SteelMark { get; set; }
        public string IngotProfile { get; set; }
        public int IngotsCount { get; set; }
        public double WeightAll { get; set; }
        public double WeightOne { get; set; }
        public int ProductCode { get; set; }
        public int IngotLenght { get; set; }
        public string Gost { get; set; }
        public int Diameter { get; set; }
        public string Customer { get; set; }
        public string Shift { get; set; }
        public string Class { get; set; }

        public LandingData()
        {
            MeltNumber = "";
            SteelMark = "";
            IngotProfile = "";
            Gost = default;
            IngotsCount = 0;
            WeightAll = 0;
            IngotLenght = 0;
            WeightOne = 0;
            ProductCode = 0;
            Diameter = default;
            Customer = default;
            Shift = default;
            Class = default;
        }
    }
}
