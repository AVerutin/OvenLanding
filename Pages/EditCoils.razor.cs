using System;
using System.Collections.Generic;
using NLog;
using OvenLanding.Data;

namespace OvenLanding.Pages
{
    public partial class EditCoils : IDisposable
    {
        private static readonly DBConnection Db = new DBConnection();
        private List<CoilData> _currentMelt = new List<CoilData>();
        // private List<CoilData> _previousMelt = new List<CoilData>();
        // private List<CoilData> _meltsToReset = new List<CoilData>();
        
        protected override void OnInitialized()
        {
            Initialize();
        }

        public void Dispose()
        {
        }

        private void Initialize()
        {
            bool current = false;
            
            // Получаем очередь на посаде
            var currMelts = Db.GetLandingOrder();
            foreach (LandingData melt in currMelts)
            {
                if (melt.Weighted > 0)
                {
                    current = true;
                }
            }
            
            _currentMelt = Db.GetCoilData(current);
            
            // _previousMelt = Db.GetCoilData(false);
            // foreach (CoilData prev in _previousMelt)
            // {
            //     _meltsToReset.Add(prev);
            // }
            //
            // foreach (CoilData curr in _currentMelt)
            // {
            //     _meltsToReset.Add(curr);
            // }
            
            StateHasChanged();
        }

        private void ResetCoil(int coilUid)
        {
            Db.ResetCoil(coilUid);
            _currentMelt = Db.GetCoilData();
            // _previousMelt = Db.GetCoilData(false);
            StateHasChanged();
        }
    }
}