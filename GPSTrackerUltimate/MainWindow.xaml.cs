using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DMISharp;
using GPSTrackerUltimate.Types;
using GPSTrackerUltimate.Types.Byond;
using GPSTrackerUltimate.Types.Helpers;
using GPSTrackerUltimate.Types.Map;
using GPSTrackerUltimate.ViewModels;

namespace GPSTrackerUltimate;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public MainViewModel MainViewModal { get; set; } 
    public MainWindow()
    {
        InitializeComponent();
        MainViewModal = new MainViewModel();
        DataContext = MainViewModal;

        Loaded += async (_, _) => await MainViewModal.LoadMapAsync();
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[index : 0] is Tile selectedTile)
        {
            double offsetX = selectedTile.X * 32 - MapScrollViewer.ViewportWidth / 2;
            double offsetY = selectedTile.Y * 32 - MapScrollViewer.ViewportHeight / 2;

            MapScrollViewer.ScrollToHorizontalOffset(offset : offsetX);
            MapScrollViewer.ScrollToVerticalOffset(offset : offsetY);
        }
    }

    private void SearchTilesByName(
        object sender,
        RoutedEventArgs e )
    {
        MainViewModal.SearchTilesByName(  );
    }

}
