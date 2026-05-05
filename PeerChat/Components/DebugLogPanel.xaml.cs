using PeerChat.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PeerChat.Components
{
    public partial class DebugLogPanel : UserControl
    {
        public DebugLogPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DebugListProperty =
            DependencyProperty.Register(
                nameof(DebugList),
                typeof(ObservableCollection<DebugLogModel>),
                typeof(DebugLogPanel),
                new PropertyMetadata(null)
            );

        public ObservableCollection<DebugLogModel> DebugList
        {
            get => (ObservableCollection<DebugLogModel>)GetValue(DebugListProperty);
            set => SetValue(DebugListProperty, value);
        }
    }
}