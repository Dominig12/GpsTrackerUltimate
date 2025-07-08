using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GPSTrackerUltimate.Types.Object
{

    public class ProgressBarInfo : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        private int _progressMaximum;
        public int ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                _progressMaximum = value;
                OnPropertyChanged();
            }
        }

        public void SetData(
            int current,
            int max )
        {
            ProgressValue = current;
            ProgressMaximum = max;
        }

        public void Increment()
        {
            ProgressValue++;
        }

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null )
        {
            PropertyChanged?.Invoke(
                sender : this,
                e : new PropertyChangedEventArgs( propertyName : propertyName ) );
        }

        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null )
        {
            if ( EqualityComparer<T>.Default.Equals(
                    x : field,
                    y : value ) )
                return false;

            field = value;
            OnPropertyChanged( propertyName : propertyName );

            return true;
        }

    }

}
