using PeerChat.Models;
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
    /// Interaction logic for DebugLogPanel.xaml
    /// </summary>
    public partial class DebugLogPanel : UserControl
    {
        public DebugLogPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DataListProperty = DependencyProperty.Register(
            nameof(DataList),
            typeof(List<MessageModel>),
            typeof(DebugLogPanel)
            );

        public List<MessageModel> DataList
        {
            get => (List<MessageModel>)GetValue(DataListProperty);
            set => SetValue(DataListProperty, value);
        }

    }
}
