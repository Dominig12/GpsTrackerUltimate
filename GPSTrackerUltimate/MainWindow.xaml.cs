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

    public MainWindow()
    {
        InitializeComponent();
        MainViewModel model = new MainViewModel();
        DataContext = model;

        Loaded += async (_, _) => await model.LoadMapAsync();
    }

}
