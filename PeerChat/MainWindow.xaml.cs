using PeerChat.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace PeerChat
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }


        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if(DataContext is MainViewModel mainVm && mainVm.CurrentView is ChatViewModel vm)
            {
                await vm.DisconnectAsync();
            }
        }

    }
}