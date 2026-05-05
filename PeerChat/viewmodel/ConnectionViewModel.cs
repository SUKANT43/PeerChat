using PeerChat.Base;
using PeerChat.Enums;
using PeerChat.Services;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PeerChat.ViewModel
{
    public class ConnectionViewModel : Observable
    {
        private readonly MainViewModel _mainViewModel;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private bool _isWaiting;

        public ConnectionViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            HostCommand = new RelayCommand(async o => await Host());
            JoinCommand = new RelayCommand(async o => await Join());
            CancelHostCommand = new RelayCommand(o => CancelHost());
            SelectHostCommand = new RelayCommand(o => SelectHost());
            SelectJoinCommand = new RelayCommand(o => SelectJoin());
            CopyAddressCommand = new RelayCommand(o => CopyAddress());

            Role = UserRole.User;
            Port = 9000;
            Name = Environment.UserName;
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private UserRole _role;
        public UserRole Role
        {
            get => _role;
            set
            {
                if (_role == value) return;
                _role = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsUser));
                OnPropertyChanged(nameof(IsIPEnabled));
            }
        }

        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsUser => Role == UserRole.User;
        public bool IsIPEnabled => Role == UserRole.User;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _connectionIPAdress;
        public string ConnectionIPAdress
        {
            get => _connectionIPAdress;
            set
            {
                _connectionIPAdress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HostDisplayAddress));
            }
        }

        private int _port;
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HostDisplayAddress));
            }
        }

        public string HostDisplayAddress => $"{ConnectionService.GetLocalIPAddress()}:{Port}";

        public bool IsWaiting
        {
            get => _isWaiting;
            set
            {
                _isWaiting = value;
                OnPropertyChanged();
            }
        }

        public ICommand HostCommand { get; }
        public ICommand JoinCommand { get; }
        public ICommand SelectHostCommand { get; }
        public ICommand SelectJoinCommand { get; }
        public ICommand CancelHostCommand { get; }
        public ICommand CopyAddressCommand { get; }

        private void SelectHost()
        {
            Role = UserRole.Admin;
            ConnectionIPAdress = ConnectionService.GetLocalIPAddress();
            ErrorMessage = null;
        }

        private void SelectJoin()
        {
            Role = UserRole.User;
            ConnectionIPAdress = "";
            ErrorMessage = null;
        }

        private void CopyAddress()
        {
            Clipboard.SetText(HostDisplayAddress);
        }

        private async Task Host()
        {
            if (!ValidateCommon()) return;

            _cts = new CancellationTokenSource();

            try
            {
                _listener = ConnectionService.StartHost(Port);
                ErrorMessage = null;
                IsWaiting = true;

                var acceptTask = ConnectionService.WaitForClientAsync(_listener);
                var completed = await Task.WhenAny(acceptTask, Task.Delay(-1, _cts.Token));

                if (completed != acceptTask)
                {
                    IsWaiting = false;
                    _listener.Stop();
                    ErrorMessage = "Hosting cancelled.";
                    return;
                }

                var client = await acceptTask;
                IsWaiting = false;
                _listener.Stop();

                _mainViewModel.NavigateToChat(client, Name, Role);
            }
            catch (Exception ex)
            {
                IsWaiting = false;
                ErrorMessage = ex.Message;
            }
        }

        private async Task Join()
        {
            if (!ValidateCommon()) return;

            if (string.IsNullOrWhiteSpace(ConnectionIPAdress))
            {
                ErrorMessage = "IP Address is required.";
                return;
            }

            if (!IPAddress.TryParse(ConnectionIPAdress, out var ip) ||
                ip.AddressFamily != AddressFamily.InterNetwork)
            {
                ErrorMessage = "Enter a valid IPv4 address.";
                return;
            }

            try
            {
                ErrorMessage = null;
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var client = await ConnectionService.ConnectAsync(ConnectionIPAdress, Port, cts.Token);
                _mainViewModel.NavigateToChat(client, Name, Role);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void CancelHost()
        {
            _cts?.Cancel();
            IsWaiting = false;
        }

        private bool ValidateCommon()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Username is required.";
                return false;
            }

            if (Name.Length < 2 || Name.Length > 20)
            {
                ErrorMessage = "Username must be between 2 and 20 characters.";
                return false;
            }

            if (Port < 1024 || Port > 65535)
            {
                ErrorMessage = "Port must be between 1024 and 65535.";
                return false;
            }

            if (IsAdmin && ConnectionService.IsPortInUse(Port))
            {
                ErrorMessage = "Port is already in use.";
                return false;
            }

            return true;
        }
    }
}