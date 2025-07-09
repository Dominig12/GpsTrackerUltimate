using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Size = SixLabors.ImageSharp.Size;

namespace GPSTrackerUltimate.Types.Helpers
{

    public static class ImageHelper
    {

        private static ConcurrentDictionary<string, BitmapImage?> Icons = new ConcurrentDictionary<string, BitmapImage?>();

        private static string GetKey(
            string icon,
            string iconState,
            StateDirection direction )
        {
            return $"{icon}_{iconState}_{direction}";
        }
        
        public static async Task<BitmapImage?> GetIconDmi(string icon, string iconState, StateDirection direction)
        {
            string key = GetKey(
                icon : icon,
                iconState : iconState,
                direction : direction );
            if ( ImageHelper.Icons.ContainsKey(
                    key :key ) )
            {
                return ImageHelper.Icons[key : key ].Clone();
            }
            
            try
            {
                using DMIFile file = new DMIFile(
                    file : $"C:\\Users\\DaniilAliskandarov\\Documents\\GitHub\\TauCetiClassic\\{icon}" );

                List<DMIState> state = file.States.Where( predicate : x => x.Name == iconState )
                    .ToList();

                if ( state.Count == 0 )
                {
                    Console.WriteLine(value : $"[ERROR] No state for icon {icon} {iconState} {direction}" );
                    return null;
                }

                if ( state[index : 0].Frames == 0 )
                {
                    Console.WriteLine(value : $"[ERROR] No frames for icon {icon} {iconState} {direction}" );
                    return null;
                }

                Image<Rgba32>? frame = null;

                try
                {
                    frame = state[index : 0].GetFrame(direction : direction, frame: 0);
                }
                catch ( Exception e)
                {
                    Console.WriteLine(value : $"[ERROR] Failed to get frame for icon {icon} {iconState} {direction} {e.ToString()}" );
                }

                if ( frame == null )
                {
                    Console.WriteLine(value : $"[ERROR] Failed to get frame for icon {icon} {iconState} {direction}" );
                    return null;
                }
            
                using MemoryStream ms = new MemoryStream();
                await frame.SaveAsPngAsync( stream : ms );
            
                BitmapImage iconBit = new BitmapImage();
            
                iconBit.BeginInit();
                iconBit.CacheOption = BitmapCacheOption.OnLoad;
                iconBit.StreamSource = ms;
                iconBit.EndInit();

                BitmapImage? result = ImageHelper.MakeWhitePixelsTransparent( original : iconBit );
                
                ImageHelper.Icons.TryAdd(
                    key : key,
                    value : result );

                return result;
            }
            catch ( Exception e )
            {
                Console.WriteLine(value : $"[ERROR] Failed to get icon {icon} {iconState} {direction} {e.ToString()}" );
                return null;
            }
        }
        
        public static WriteableBitmap? CombineImages(IEnumerable<BitmapImage> images, int width = 32, int height = 32)
        {
            try
            {
                WriteableBitmap wb = new WriteableBitmap(pixelWidth : width, pixelHeight : height, dpiX : 96, dpiY : 96, pixelFormat : PixelFormats.Pbgra32, palette : null);

                foreach (BitmapImage img in images)
                {
                    if (img == null || img.PixelWidth == 0 || img.PixelHeight == 0)
                    {
                        continue;
                    }

                    Int32Rect rect = new Int32Rect(x : 0, y : 0, width : Math.Min(val1 : width, val2 : img.PixelWidth), height : Math.Min(val1 : height, val2 : img.PixelHeight));
                    int stride = rect.Width * 4;
                    byte[] pixels = new byte[rect.Height * stride];

                    try
                    {
                        img.CopyPixels(sourceRect : rect, pixels : pixels, stride : stride, offset : 0);
                        wb.WritePixels(sourceRect : rect, pixels : pixels, stride : stride, offset : 0);
                    }
                    catch
                    {
                        // Пропускаем битые изображения
                    }
                }


                return wb;
            }
            catch ( Exception )
            {
                //
                return null;
            }
        }
        
        public static BitmapImage? ConvertToBitmapImage(BitmapSource? source)
        {
            if (source == null)
            {
                return null;
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(item : BitmapFrame.Create(source : source));

            using MemoryStream memoryStream = new MemoryStream();

            encoder.Save(stream : memoryStream);
            memoryStream.Position = 0;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();

            return image;

        }
        
        public static BitmapImage? MakeWhitePixelsTransparent(BitmapImage original)
        {
            const byte Tolerance = 5;

            // Копируем BitmapImage в WriteableBitmap
            WriteableBitmap wb = new WriteableBitmap(source : original);

            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels : pixels, stride : stride, offset : 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                // Если цвет почти белый (учитываем допуск)
                if (r >= 255 - Tolerance && g >= 255 - Tolerance && b >= 255 - Tolerance)
                {
                    // Сделать прозрачным
                    pixels[i + 3] = 0; // Alpha
                }
            }

            WriteableBitmap wbTransparent = new WriteableBitmap(pixelWidth : width, pixelHeight : height, dpiX : 96, dpiY : 96, pixelFormat : PixelFormats.Pbgra32, palette : null);
            wbTransparent.WritePixels(sourceRect : new Int32Rect(x : 0, y : 0, width : width, height : height), pixels : pixels, stride : stride, offset : 0);

            // Конвертируем в BitmapImage
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(item : BitmapFrame.Create(source : wbTransparent));

            using MemoryStream ms = new MemoryStream();
            encoder.Save(stream : ms);
            ms.Position = 0;

            BitmapImage? result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.StreamSource = ms;
            result.EndInit();
            result.Freeze();

            return result;
        }

    }

}
