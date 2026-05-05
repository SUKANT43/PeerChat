using PeerChat.ViewModel;
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
    }
}