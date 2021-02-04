using System;

namespace OvenLanding.Data
{
    public class AreasData
    {
        public int LandingId { get; set; }
        public DateTime LandingDate { get; set; }
        public DateTime DateEnter { get; set; }
        public int Thread { get; set; }
        public int NodeId { get; set; }
        public string NodeName { get; set; }
        public string MeltNumber { get; set; }
        public double Diameter { get; set; }
        public int Position { get; set; }
        public int CountPosad { get; set; }
        public int BilletWeight { get; set; }

        public AreasData()
        {
            LandingId = default;
            DateEnter = DateTime.MinValue;
            Thread = default;
            NodeId = default;
            NodeName = default;
            MeltNumber = default;
            Diameter = default;
            LandingDate = DateTime.MinValue;
            Position = default;
            CountPosad = default;
            BilletWeight = default;
        }
    }
}