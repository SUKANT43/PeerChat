using PeerChat.Enums;
using PeerChat.models;
using PeerChat.Protocol;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PeerChat.Services
{
    public class ChatService
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public ChatService(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        public async Task SendNameAsync(string name)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(name);
                await MessageProtocol.SendFrameAsync(_stream, (byte)MessageType.Text, data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending name: {ex.Message}");
            }
        }

        public async Task<string> ReceiveNameAsync()
        {
            try
            {
                MessageFrameModel frame = await MessageProtocol.ReceiveFrameAsync(_stream);

                if (frame == null || (MessageType)frame.Type != MessageType.Text)
                    throw new Exception("Expected name as first message");

                return Encoding.UTF8.GetString(frame.Payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving name: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task SendMessageAsync(byte type, byte[] payload)
        {
            try
            {
                await MessageProtocol.SendFrameAsync(_stream, type, payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        public async Task<MessageFrameModel> ReceiveMessageAsync()
        {
            try
            {
                return await MessageProtocol.ReceiveFrameAsync(_stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving message: {ex.Message}");
                return null;
            }
        }
    }
}