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
    /// Interaction logic for VideoMessageComponent.xaml
    /// </summary>
    public partial class VideoMessageComponent : UserControl
    {
        public VideoMessageComponent()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ThumbnailProperty =
            DependencyProperty.Register(nameof(Thumbnail)
                ,typeof(BitmapImage),
                typeof(VideoMessageComponent));

        public BitmapImage Thumbnail
        {
            get => (BitmapImage)GetValue(ThumbnailProperty);
            set => SetValue(ThumbnailProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress), 
            typeof(double), 
            typeof(VideoMessageComponent));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty IsReceivingProperty =DependencyProperty.Register(
            nameof(IsReceiving), 
            typeof(bool), 
            typeof(VideoMessageComponent));

        public bool IsReceiving
        {
            get => (bool)GetValue(IsReceivingProperty);
            set => SetValue(IsReceivingProperty, value);
        }


        public static readonly DependencyProperty IsSendingProperty = DependencyProperty.Register(
            nameof(IsSending),
            typeof(bool),
            typeof(VideoMessageComponent));

        public bool IsSending
        {
            get => (bool)GetValue(IsSendingProperty);
            set => SetValue(IsSendingProperty, value);
        }

    }
}