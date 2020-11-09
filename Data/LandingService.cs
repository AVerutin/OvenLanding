using System.ComponentModel;
using System.Runtime.CompilerServices;
using OvenLanding.Annotations;

namespace OvenLanding.Data
{
    public class LandingService : INotifyPropertyChanged
    {
        private LandingData _editableDate;
        private LandingData _originalData;
        public int MeltUid { get; private set; }
        public bool EditMode {get; private set; }
        
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

        public void SetEditable(LandingData original, LandingData editable)
        {
            EditMode = true;
            MeltUid = editable.LandingId;
            _editableDate = editable;
            _originalData = original;
        }

        public LandingData GetEditable() => _editableDate;
        public LandingData GetOriginal() => _originalData;

        public void ClearEditable()
        {
            EditMode = false;
            MeltUid = 0;
            _editableDate = new LandingData();
        }

    }
}