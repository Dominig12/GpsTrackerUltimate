using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GPSTrackerUltimate
{

    public class GridToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int grid)
            {
                return grid * 32; // Размер тайла
            }

            return 0;
        }

        object? IValueConverter.ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture )
        {
            return ConvertBack( value : value,
                targetType : targetType,
                parameter : parameter,
                culture : culture );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

    }

}