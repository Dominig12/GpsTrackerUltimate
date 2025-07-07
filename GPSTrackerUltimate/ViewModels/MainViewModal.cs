using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DMISharp;
using GPSTrackerUltimate.Types;
using GPSTrackerUltimate.Types.Byond;
using GPSTrackerUltimate.Types.Map;
using SixLabors.ImageSharp;

namespace GPSTrackerUltimate.ViewModels
{

    public class MainViewModel
    {

        public ObservableCollection<Tile> Tiles { get; set; }
        
        public MainViewModel()
        {

            Tiles = new ObservableCollection<Tile>();
            
            // 3. Парсим карту
            DmmParser dmmParser = new DmmParser();

            var tiles = dmmParser.Parse(
                path :
                "C:\\Users\\DaniilAliskandarov\\Documents\\GitHub\\TauCetiClassic\\maps\\boxstation\\boxstation.dmm" );

            foreach ( Tile tile in tiles )
            {
                Tiles.Add(item : tile);
            }
        }

// Загрузи данные и начни отрисовку
        public async Task LoadMapAsync()
        {

            // 1. Загружаем список DM файлов из DME
            var dmePath = "C:\\Users\\DaniilAliskandarov\\Documents\\GitHub\\TauCetiClassic\\taucetistation.dme";
            var dmFiles = DmeParser.ParseIncludedDmFiles( dmePath : dmePath );

            // 2. Парсим все объекты
            var allObjects = DmParser.ParseObjectsFromFiles( dmFilePaths : dmFiles );

            var vars = DmParser.FindObjectWithResolvedVars(
                "/obj/structure/object_wall/mining",
                allObjects );
            
            
            
            // 4. Постепенная отрисовка
            foreach ( var tile in Tiles )
            {

                _ = Task.Run(
                    function : async () =>
                    {
                        await TileRenderer.ProcessTileAsync(
                            tile : tile,
                            allObjects : allObjects );

                        // // Если используешь PropertyChanged — уведомить, если надо
                        // Application.Current.Dispatcher.Invoke(
                        //     callback : () =>
                        //     {
                        //         PropertyChanged?.Invoke(
                        //             sender : this,
                        //             e : new PropertyChangedEventArgs( propertyName : nameof( MainViewModel.Tiles ) ) );
                        //     } );
                    } );

                //await Task.Delay( millisecondsDelay : 1 ); // плавная загрузка
            }
        }

    }

}
