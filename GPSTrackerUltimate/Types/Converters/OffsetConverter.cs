using System.Globalization;
using System.Windows.Data;

namespace GPSTrackerUltimate.Types.Converters
{

    public class OffsetConverter : IMultiValueConverter
    {
        private const int TileSize = 32;
        private const int EllipseSize = 20;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || !(values[0] is int x) || !(values[1] is int y))
                return 0.0;

            x--;
            y--;

            // Определяем смещение
            double offset = (TileSize - EllipseSize) / 2.0;

            if (parameter?.ToString() == "X")
                return x * TileSize + offset;
            else if (parameter?.ToString() == "Y")
                return y * TileSize + offset;

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

}
