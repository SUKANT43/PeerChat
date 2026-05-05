using PeerChat.Base;
using PeerChat.Enums;
using System.Net.Sockets;

namespace PeerChat.ViewModel
{
    public class MainViewModel : Observable
    {
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            NavigateToConnection();
        }

        public void NavigateToConnection()
        {
            CurrentView = new ConnectionViewModel(this);
        }

        public void NavigateToChat(TcpClient client, string name, UserRole role)
        {
            CurrentView = new ChatViewModel(this, client, name, role);
        }
    }
}