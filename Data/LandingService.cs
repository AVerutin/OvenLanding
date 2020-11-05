using System.ComponentModel;
using System.Runtime.CompilerServices;
using OvenLanding.Annotations;

namespace OvenLanding.Data
{
    public class LandingService : INotifyPropertyChanged
    {
        public LandingData EditableDate { get; set; }
        public bool EditMode = false;
        
        private int _ingotsCount; 
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

    }
}