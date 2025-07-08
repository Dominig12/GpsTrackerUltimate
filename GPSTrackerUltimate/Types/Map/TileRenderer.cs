using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DMISharp;
using GPSTrackerUltimate.Types.Byond;
using GPSTrackerUltimate.Types.Enum;
using GPSTrackerUltimate.Types.Helpers;

namespace GPSTrackerUltimate.Types.Map
{

    public static class TileRenderer
    {
        /// <summary>
        /// Обрабатывает тайл: загружает иконки и комбинирует их в один
        /// </summary>
        public static async Task ProcessTileAsync(
            Tile tile,
            Dictionary<string, DmObject> allObjects)
        {
            tile.TileContent.Clear();

            List<BitmapImage> images = new List<BitmapImage>();

            foreach (KeyValuePair<int, string> kv in tile.PathContent.OrderByDescending(keySelector : p => p.Key))
            {
                int layer = kv.Key;
                string typePath = kv.Value;

                if (typePath.StartsWith(value : "/area"))
                {
                    continue;
                }

                (DmObject Obj, Dictionary<string, string> ResolvedVars)? objResult = DmParser.FindObjectWithResolvedVars(typePath : typePath, allObjects : allObjects);
                if (objResult == null)
                {
                    continue;
                }

                (_, Dictionary<string, string> vars) = objResult.Value;

                // === Ищем переопределения, если есть ===
                string icon = vars.TryGetValue(key : "icon", value : out string? iconVal) ? iconVal.Trim(trimChar : '\'') : string.Empty;
                string iconState = vars.TryGetValue(key : "icon_state", value : out string? iconStateVal) ? iconStateVal.Trim(trimChar : '"') : string.Empty;
                string direction = vars.TryGetValue(key : "dir", value : out string? directionVal) ? directionVal.Trim(trimChar : '"') : string.Empty;
                string name = vars.TryGetValue(key : "name", value : out string? nameVal) ? nameVal.Trim(trimChar : '\'') : string.Empty;

                if ( !string.IsNullOrWhiteSpace( value : name ) )
                {
                    tile.NameContent.Add(item : name);
                }

                string layerPrefix = $"{layer}.";

                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "icon", value : out string? overrideIcon))
                {
                    icon = overrideIcon.Trim(trimChar : '\'');
                }

                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "icon_state", value : out string? overrideIconState))
                {
                    iconState = overrideIconState.Trim(trimChar : '"');
                }

                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "dir", value : out string? overrideDirection))
                {
                    direction = overrideDirection.Trim(trimChar : '"');
                }

                // === Проверка ===
                if (string.IsNullOrWhiteSpace(value : icon))
                {
                    Console.WriteLine(value : $"[WARN] Не найдена icon для {typePath}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value : iconState))
                {
                    Console.WriteLine(value : $"[WARN] Не найден icon_state для {typePath}");
                    continue;
                }
                
                DirectionBYond directionE = System.Enum.TryParse(value : direction, result : out DirectionBYond directionVar) ? directionVar : DirectionBYond.South;

                // === Загрузка изображения ===
                try
                {
                    BitmapImage image = await ImageHelper.GetIconDmi(icon : icon, iconState : iconState, direction : ConverterDirection.ConvertByondDirToDmi( dir : directionE ));
                    tile.TileContent[key : layer] = image;
                    images.Add(item : image);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(value : $"[ERROR] Ошибка при загрузке иконки {icon}:{iconState} — {ex.Message}");
                }
            }

            tile.ImageTileCombine = CombineImages(images : images);
            
            StringBuilder tooltipBuilder = new System.Text.StringBuilder();

            foreach (KeyValuePair<int, string> kv in tile.PathContent.OrderByDescending(keySelector : p => p.Key))
            {
                int layer = kv.Key;
                string typePath = kv.Value;

                if (allObjects.TryGetValue(key : typePath, value : out DmObject? obj))
                {
                    tooltipBuilder.AppendLine(handler : $"{typePath}");

                    foreach ((string key, string val) in obj.GetAllResolvedVariables(allObjects : allObjects))
                    {
                        tooltipBuilder.AppendLine(handler : $"  {key} = {val}");
                    }

                    tooltipBuilder.AppendLine();
                }
            }

            tile.ToolTipText = tooltipBuilder.ToString().Trim();
        }

        /// <summary>
        /// Комбинирует изображения в один BitmapImage
        /// </summary>
        public static BitmapImage? CombineImages(IEnumerable<BitmapImage> images)
        {
            const int size = 32;

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext context = drawingVisual.RenderOpen())
            {
                foreach (BitmapImage img in images)
                {
                    try
                    {
                        if (img == null || img.PixelWidth == 0 || img.PixelHeight == 0)
                        {
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    context.DrawImage(imageSource : img, rectangle : new Rect(x : 0, y : 0, width : size, height : size));
                }
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(pixelWidth : size, pixelHeight : size, dpiX : 96, dpiY : 96, pixelFormat : PixelFormats.Pbgra32);
            rtb.Render(visual : drawingVisual);

            // Преобразуем в BitmapImage
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(item : BitmapFrame.Create(source : rtb));
            using MemoryStream stream = new MemoryStream();
            encoder.Save(stream : stream);
            stream.Position = 0;

            BitmapImage final = new BitmapImage();
            final.BeginInit();
            final.CacheOption = BitmapCacheOption.OnLoad;
            final.StreamSource = stream;
            final.EndInit();
            final.Freeze();

            return final;
        }
        
        public static async Task DrawImageOnWriteableBitmap(
            WriteableBitmap? target,
            BitmapImage? source,
            int x,
            int y)
        {
            try
            {
                if (source == null || target == null)
                {
                    return;
                }

                // Конвертируем BitmapImage в формат Pbgra32
                FormatConvertedBitmap converted = new FormatConvertedBitmap(source : source, destinationFormat : PixelFormats.Pbgra32, destinationPalette : null, alphaThreshold : 0);
                converted.Freeze();

                WriteableBitmap wb = new WriteableBitmap(source : converted);
                wb.Freeze();

                int width = wb.PixelWidth;
                int height = wb.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                wb.CopyPixels(pixels : pixels, stride : stride, offset : 0);

                await Application.Current.Dispatcher.InvokeAsync(callback : () =>
                {
                    target.Lock();
                    target.WritePixels(
                        sourceRect : new Int32Rect(x : x, y : y, width : width, height : height),
                        pixels : pixels,
                        stride : stride,
                        offset : 0);
                    target.AddDirtyRect(dirtyRect : new Int32Rect(x : x, y : y, width : width, height : height));
                    target.Unlock();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(value : $"Ошибка DrawImageOnWriteableBitmap: {ex.Message}");
            }
        }
    }

}
