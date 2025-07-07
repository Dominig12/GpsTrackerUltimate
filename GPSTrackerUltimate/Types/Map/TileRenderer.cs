using System.IO;
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

            var images = new List<BitmapImage>();

            foreach (var kv in tile.PathContent.OrderByDescending(keySelector : p => p.Key))
            {
                var layer = kv.Key;
                var typePath = kv.Value;

                if (typePath.StartsWith(value : "/area"))
                    continue;

                var objResult = DmParser.FindObjectWithResolvedVars(typePath : typePath, allObjects : allObjects);
                if (objResult == null)
                    continue;

                var (_, vars) = objResult.Value;

                // === Ищем переопределения, если есть ===
                string icon = vars.TryGetValue(key : "icon", value : out var iconVal) ? iconVal.Trim(trimChar : '\'') : string.Empty;
                string iconState = vars.TryGetValue(key : "icon_state", value : out var iconStateVal) ? iconStateVal.Trim(trimChar : '"') : string.Empty;
                string direction = vars.TryGetValue(key : "dir", value : out var directionVal) ? directionVal.Trim(trimChar : '"') : string.Empty;

                string layerPrefix = $"{layer}.";

                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "icon", value : out var overrideIcon))
                    icon = overrideIcon.Trim(trimChar : '\'');

                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "icon_state", value : out var overrideIconState))
                    iconState = overrideIconState.Trim(trimChar : '"');
                
                if (tile.PathOverrides.TryGetValue(key : layerPrefix + "dir", value : out var overrideDirection))
                    direction = overrideDirection.Trim(trimChar : '"');

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
                    var image = await ImageHelper.GetIconDmi(icon : icon, iconState : iconState, direction : ConverterDirection.ConvertByondDirToDmi( dir : directionE ));
                    tile.TileContent[key : layer] = image;
                    images.Add(item : image);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(value : $"[ERROR] Ошибка при загрузке иконки {icon}:{iconState} — {ex.Message}");
                }
            }

            tile.ImageTileCombine = CombineImages(images : images);
            
            var tooltipBuilder = new System.Text.StringBuilder();

            foreach (var kv in tile.PathContent.OrderByDescending(p => p.Key))
            {
                var layer = kv.Key;
                var typePath = kv.Value;

                if (allObjects.TryGetValue(typePath, out var obj))
                {
                    tooltipBuilder.AppendLine($"{typePath}");

                    foreach (var (key, val) in obj.GetAllResolvedVariables(allObjects))
                    {
                        tooltipBuilder.AppendLine($"  {key} = {val}");
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

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                foreach (var img in images)
                {
                    context.DrawImage(imageSource : img, rectangle : new Rect(x : 0, y : 0, width : size, height : size));
                }
            }

            var rtb = new RenderTargetBitmap(pixelWidth : size, pixelHeight : size, dpiX : 96, dpiY : 96, pixelFormat : PixelFormats.Pbgra32);
            rtb.Render(visual : drawingVisual);

            // Преобразуем в BitmapImage
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(item : BitmapFrame.Create(source : rtb));
            using var stream = new MemoryStream();
            encoder.Save(stream : stream);
            stream.Position = 0;

            var final = new BitmapImage();
            final.BeginInit();
            final.CacheOption = BitmapCacheOption.OnLoad;
            final.StreamSource = stream;
            final.EndInit();
            final.Freeze();

            return final;
        }
    }

}
