using System;
using System.IO;
using System.Text;
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
    }
}