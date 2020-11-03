using System.ComponentModel;
using System.Runtime.CompilerServices;
using OvenLanding.Annotations;

namespace OvenLanding.Data
{
    public class LandingService : INotifyPropertyChanged
    {
        public LandingTable EditableDate { get; set; }
        public bool EditMode = false;
        
        private int _ingotsCount; 
        private LandingData _savedState = new LandingData();
        public event PropertyChangedEventHandler PropertyChanged;
        
        public int IngotsCount
        {
            get => _ingotsCount;
            set
            {
                if (value != IngotsCount)
                {
                    _ingotsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SaveState(LandingData state)
        {
            _savedState = state;
            _savedState.MeltNumber = "";
            _savedState.IngotsCount = 0;
        }

        public LandingData GetState()
        {
            if (_savedState == null)
            {
                _savedState = new LandingData();
            }

            return _savedState;
        }
    }
}