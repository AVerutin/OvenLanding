namespace OvenLanding.Data
{
    public class LandingData
    {
        public long Number { get; set; }
        public string SteelMark { get; set; }
        public string IngotProfile { get; set; }
        public int LegalCount { get; set; }
        public double LegalWeight { get; set; }
        public int Lenght { get; set; }
        public double Weight { get; set; }

        public LandingData()
        {
            Number = 0;
            SteelMark = "";
            IngotProfile = "";
            LegalCount = 0;
            LegalWeight = 0;
            Lenght = 0;
            Weight = 0;
        }
    }
}