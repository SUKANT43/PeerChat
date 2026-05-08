using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PeerChat.Helper
{
    public static class FileHelper
    {
        public static byte[] EncodeImagePayload(string fileName, byte[] fileBytes)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);

            if (nameBytes.Length > 260)
                throw new ArgumentException("Filename exceeds 260 bytes");

            byte[] header = new byte[260];
            Array.Copy(nameBytes, header, nameBytes.Length);

            byte[] payload = new byte[260 + fileBytes.Length];

            Buffer.BlockCopy(header, 0, payload, 0, 260);
            Buffer.BlockCopy(fileBytes, 0, payload, 260, fileBytes.Length);

            return payload;
        }

        public static void DecodeImagePayload(byte[] payLoad, out byte[] imageData, out string fileName)
        {
            if (payLoad.Length < 260)
                throw new Exception("Invalid payload");

            byte[] fileNameBytes = new byte[260];
            Buffer.BlockCopy(payLoad, 0, fileNameBytes, 0, 260);

            fileName = Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0');

            int imageLength = payLoad.Length - 260;
            imageData = new byte[imageLength];

            Buffer.BlockCopy(payLoad, 260, imageData, 0, imageLength);
        }

        public static BitmapImage ConvertToImage(byte[] imageData)
        {
            using (var ms = new MemoryStream(imageData))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();

                return image;
            }
        }

        public static async Task<BitmapImage> GenerateVideoThumbnailAsync(string videoPath)
        {
            try
            {
                var thumbnailTask = await Application.Current.Dispatcher.InvokeAsync(
                    () => GenerateVideoThumbnail(videoPath));

                return await thumbnailTask;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<BitmapImage> GenerateVideoThumbnail(string videoPath)
        {
            try
            {
                MediaPlayer player = new MediaPlayer();
                player.ScrubbingEnabled = true;

                player.Open(new Uri(videoPath, UriKind.Absolute));

                await Task.Delay(1000);

                player.Position = TimeSpan.FromMilliseconds(250);

                await Task.Delay(300);

                int width = player.NaturalVideoWidth;
                int height = player.NaturalVideoHeight;

                if (width <= 0) width = 250;
                if (height <= 0) height = 160;

                DrawingVisual visual = new DrawingVisual();

                using (DrawingContext dc = visual.RenderOpen())
                {
                    dc.DrawVideo(player, new Rect(0, 0, width, height));
                }

                RenderTargetBitmap renderBitmap =
                    new RenderTargetBitmap(
                        width,
                        height,
                        96,
                        96,
                        PixelFormats.Pbgra32);

                renderBitmap.Render(visual);

                BitmapImage image = new BitmapImage();

                using (MemoryStream ms = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    encoder.Save(ms);

                    ms.Position = 0;

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                }

                player.Close();

                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
