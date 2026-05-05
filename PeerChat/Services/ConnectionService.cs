using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PeerChat.Services
{
    public static class ConnectionService
    {
        public static string GetLocalIPAddress()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return "127.0.0.1";
                }
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public static bool IsPortInUse(int port)
        {
            TcpListener listener = null;

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                return false;
            }
            catch (SocketException)
            {
                return true;
            }
            finally
            {
                listener?.Stop();
            }
        }

        public static TcpListener StartHost(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                return listener;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start server: {ex.Message}");
            }
        }

        public static async Task<TcpClient> WaitForClientAsync(TcpListener listener)
        {
            try
            {
                return await listener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to accept client: {ex.Message}");
            }
        }

        public static async Task<TcpClient> ConnectAsync(string ip, int port, CancellationToken token)
        {
            var client = new TcpClient();

            try
            {
                var connectTask = client.ConnectAsync(ip, port);
                var delayTask = Task.Delay(TimeSpan.FromSeconds(5), token);

                var completed = await Task.WhenAny(connectTask, delayTask);

                if (completed != connectTask)
                {
                    client.Close();
                    throw new Exception("Connection timeout (5 seconds)");
                }

                await connectTask;
                return client;
            }
            catch (Exception ex)
            {
                client.Close();
                throw new Exception($"Connection failed: {ex.Message}");
            }
        }
    }
}