using PeerChat.models;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PeerChat.Protocol
{
    public static class MessageProtocol
    {
        private static readonly object _sendLock = new object();

        public static async Task SendFrameAsync(NetworkStream stream, byte type, byte[] payload)
        {
            lock (_sendLock)
            {
                if (payload == null)
                    payload = new byte[0];

                int length = payload.Length;

                byte[] header = new byte[5];

                header[0] = type;
                header[1] = (byte)(length >> 24);
                header[2] = (byte)(length >> 16);
                header[3] = (byte)(length >> 8);
                header[4] = (byte)(length);

                stream.Write(header, 0, 5);

                if (length > 0)
                    stream.Write(payload, 0, length);
            }
            await Task.CompletedTask;
        }

        public static async Task<MessageFrameModel> ReceiveFrameAsync(NetworkStream stream)
        {
            try
            {
                byte[] header = await ReadExactAsync(stream, 5);
                if (header == null || header.Length < 5)
                    return null;

                byte type = header[0];

                int length = (header[1] << 24) | (header[2] << 16) | (header[3] << 8) | header[4];

                byte[] payload;
                if (length > 0)
                    payload = await ReadExactAsync(stream, length);
                else
                    payload = new byte[0];

                return new MessageFrameModel { Type = type, Payload = payload };
            }
            catch
            {
                return null;
            }
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, length - totalRead);

                if (read == 0)
                    throw new IOException("Connection closed");

                totalRead += read;
            }

            return buffer;
        }
    }
}