using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using GPSTrackerUltimate.Types.Helpers;
using SixLabors.ImageSharp;

namespace GPSTrackerUltimate.Types.Map
{

    public class Tile : INotifyPropertyChanged
    {

        public int X { get; set; }
        
        public int Y { get; set; }
        
        public int Z { get; set; }

        public Dictionary<int, BitmapImage?> TileContent { get; set; } = new Dictionary<int, BitmapImage?>();

        public Dictionary<int, string> PathContent { get; set; } = new Dictionary<int, string>();
        
        public List<string> NameContent { get; set; } = new List<string>();
        
        public Dictionary<string, string> PathOverrides { get; set; } = new();
        
        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
            }
        }
        
        private string _tooltipText = string.Empty;
        public string ToolTipText
        {
            get => _tooltipText;
            set
            {
                if (_tooltipText != value)
                {
                    _tooltipText = value;
                    OnPropertyChanged(propertyName : nameof(ToolTipText));
                }
            }
        }
        
        private BitmapImage? _imageTileCombine;
        public BitmapImage? ImageTileCombine
        {
            get => _imageTileCombine;
            set
            {
                if (_imageTileCombine != value)
                {
                    _imageTileCombine = value;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(sender : this, e : new PropertyChangedEventArgs(propertyName : propertyName));
    }

}
