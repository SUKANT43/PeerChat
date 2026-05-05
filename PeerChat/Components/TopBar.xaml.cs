using PeerChat.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PeerChat.Components
{
    /// <summary>
    /// Interaction logic for TopBar.xaml
    /// </summary>
    public partial class TopBar : UserControl
    {
        public TopBar()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PeerNameProperty = DependencyProperty.Register(
            nameof(PeerName),
            typeof(string),
            typeof(TopBar)
            );

        public string PeerName
        {
            get => (string)GetValue(PeerNameProperty);
            set => SetValue(PeerNameProperty, value);
        }

        public static readonly DependencyProperty ToggleThemeCommandProperty = DependencyProperty.Register(
            nameof(ToggleThemeCommand), 
            typeof(ICommand), 
            typeof(TopBar));

        public ICommand ToggleThemeCommand
        {
            get => (ICommand)GetValue(ToggleThemeCommandProperty);
            set => SetValue(ToggleThemeCommandProperty, value);
        }


    }
}
