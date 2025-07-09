using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DMISharp;
using GPSTrackerUltimate.Types;
using GPSTrackerUltimate.Types.Byond;
using GPSTrackerUltimate.Types.Map;
using GPSTrackerUltimate.Types.Object;
using SixLabors.ImageSharp;
using DMCompiler;
using DMCompiler.Compiler.DM;

namespace GPSTrackerUltimate.ViewModels
{

    public class MainViewModel
    {

        public ObservableCollection<Tile> Tiles { get; set; }
        public WriteableBitmap CombinedMapImage { get; set; }
        public ProgressBarInfo ProgressBar { get; set; }
        public string SearchQuery  { get; set; } = string.Empty;
        
        public ObservableCollection<Tile> SearchResults { get; set; } = new();
        
        public int MapPixelWidth => 255*32;
        public int MapPixelHeight => 255*32;
        
        public MainViewModel()
        {

            ProgressBar = new ProgressBarInfo();
            Tiles = new ObservableCollection<Tile>();
            
            DmmParser dmmParser = new DmmParser();

            List<Tile> tiles = dmmParser.Parse(
                path :
                "C:\\Users\\DaniilAliskandarov\\Documents\\GitHub\\TauCetiClassic\\maps\\boxstation\\boxstation.dmm" );

            foreach ( Tile tile in tiles )
            {
                Tiles.Add(item : tile);
            }

            InitBitmap(
                width : 255 * 32,
                height : 255 * 32 );
        }
        
        public void InitBitmap(int width, int height)
        {
            CombinedMapImage = new WriteableBitmap(
                pixelWidth: width,
                pixelHeight: height,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: PixelFormats.Pbgra32,
                palette: null);
    
            //OnPropertyChanged(propertyName : nameof(CombinedMapImage));
        }
        
        public async Task RenderTilesInParallelAsync(Dictionary<string, DmObject> allObjects)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initialCount: Environment.ProcessorCount); // Ограничим параллелизм

            IEnumerable<Task> tasks = Tiles.Select(selector : async tile =>
            {
                await semaphore.WaitAsync();

                try
                {
                    await TileRenderer.ProcessTileAsync(tile : tile, allObjects : allObjects);

                    await Application.Current.Dispatcher.InvokeAsync(callback : () =>
                    {
                        _ = TileRenderer.DrawImageOnWriteableBitmap(
                            target: CombinedMapImage,
                            source: tile.ImageTileCombine,
                            x: (tile.X-1) * 32,
                            y: (tile.Y-1) * 32);
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks : tasks);
        }
        
        public async Task RenderTilesFastQueuedAsync(Dictionary<string, DmObject> allObjects)
        {
            int tileSize = 32;
            int threadCount = Environment.ProcessorCount - 1;

            ConcurrentQueue<Tile> queue = new ConcurrentQueue<Tile>(collection : Tiles);
            ConcurrentBag<Tile> renderedTiles = new ConcurrentBag<Tile>();
            List<Task> tasks = new List<Task>();

            ProgressBar.SetData(current : 0, max : Tiles.Count);

            // Рабочие потоки для обработки очереди
            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(item : Task.Run(function : async () =>
                {
                    while (queue.TryDequeue(result : out Tile? tile))
                    {
                        await TileRenderer.ProcessTileAsync(tile : tile, allObjects : allObjects);
                        
                        // renderedTiles.Add(item : tile);
                        _ = TileRenderer.DrawImageOnWriteableBitmap(
                            target: CombinedMapImage,
                            source: tile.ImageTileCombine,
                            x: (tile.X-1) * tileSize,
                            y: (tile.Y-1) * tileSize);
                        ProgressBar.Increment();
                    }
                }));
            }

            // Ждем завершения всех потоков
            await Task.WhenAll(tasks : tasks);

            // // Финальная отрисовка на UI
            // await Application.Current.Dispatcher.InvokeAsync(callback : () =>
            // {
            //     ProgressBar.SetData(current : 0, max : Tiles.Count);
            //     foreach (Tile tile in renderedTiles)
            //     {
            //         ProgressBar.Increment();
            //         _ = TileRenderer.DrawImageOnWriteableBitmap(
            //             target: CombinedMapImage,
            //             source: tile.ImageTileCombine,
            //             x: tile.X * tileSize,
            //             y: tile.Y * tileSize);
            //     }
            // });
        }

// Загрузи данные и начни отрисовку
        public async Task LoadMapAsync()
        {
            // 1. Загружаем список DM файлов из DME
            string dmePath = "C:\\Users\\DaniilAliskandarov\\Documents\\GitHub\\TauCetiClassic\\taucetistation.dme";
            List<string> dmFiles = DmeParser.ParseIncludedDmFiles( dmePath : dmePath );

            // 2. Парсим все объекты
            Dictionary<string, DmObject> allObjects = DmParser.ParseObjectsFromFiles(  filePaths : dmFiles );
            
            allObjects.TryGetValue("/obj/structure/stool/bed/chair/metal", out DmObject? obj);
            
            await Task.Run(
                function : async () =>
                {
                    await RenderTilesFastQueuedAsync(allObjects : allObjects);
                } );
        }

        public void SearchTilesByName()
        {
            SearchResults.Clear();

            if ( string.IsNullOrWhiteSpace( SearchQuery ) )
            {
                foreach (var tile in Tiles)
                {
                    tile.IsHighlighted = false;
                }
                return;
            }
    
            foreach (var tile in Tiles)
            {
                tile.IsHighlighted = false;

                if (tile.NameContent.Any(name => name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)))
                {
                    tile.IsHighlighted = true;
                    SearchResults.Add(tile);
                }
            }
        }

    }

}
