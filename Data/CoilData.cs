using System;
using Microsoft.AspNetCore.Mvc;

namespace OvenLanding.Data
{
    public class CoilData
    {
        public int PosadUid { get; set; }            // c_id_posad [numeric]
        public string MeltNumber { get; set; }       // c_melt [text]
        public string SteelMark { get; set; }        // c_steel_grade [text]
        public string IngotProfile { get; set; }     // c_section [text]
        public int IngotsCount { get; set; }         // c_count [numeric]
        public int WeightAll { get; set; }           // c_weight_all [numeric]
        public int WeightOne { get; set; }           // c_weight_one [numeric]
        public int IngotLength { get; set; }         // c_length [numeric]
        public string Standart { get; set; }         // c_gost [text]
        public double Diameter { get; set; }         // c_diameter [numeric]
        public string Customer { get; set; }         // c_customer [text]
        public string Shift { get; set; }            // c_shift [text]
        public string Class { get; set; }            // c_class [text]
        public int ProductionCode { get; set; }      // c_prod_code [numeric]

        public int CoilUid { get; set; }             // c_id_coil [numeric], -- идентификатор бунта
        public int CoilPos { get; set; }             // c_pos [numeric], -- номер пп внутри посада
        public int CoilNumber { get; set; }          // c_num_coil [numeric],-- номер бунта, присвоенный при взвешивании (начинается со 101)
        public int WeightFact { get; set; }          // c_weight_fact [numeric], -- вес фактический
        public DateTime DateReg { get; set; }        // c_date_reg [timestamp], -- дата регистрации посада
        public DateTime DateWeight { get; set; }     // c_date_weight [timestamp] -- время взвешивания
        public bool NeedToReset { get; set; }        // Признак сброса веса бунта и его перевески

        public CoilData()
        {
            PosadUid = default;
            MeltNumber = default;
            SteelMark = default;
            IngotProfile = default;
            IngotsCount = default;
            WeightAll = default;
            WeightOne = default;
            IngotLength = default;
            Standart = default;
            Diameter = default;
            Customer = default;
            Shift = default;
            Class = default;
            ProductionCode = default;
            CoilUid = default;
            CoilPos = default;
            CoilNumber = default;
            WeightFact = default;
            DateReg = default;
            DateWeight = default;
            NeedToReset = default;
        }
    }
}
