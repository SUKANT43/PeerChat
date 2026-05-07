using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
            DependencyProperty.Register(nameof(Thumbnail), typeof(BitmapImage), typeof(VideoMessageComponent));

        public BitmapImage Thumbnail
        {
            get => (BitmapImage)GetValue(ThumbnailProperty);
            set => SetValue(ThumbnailProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(VideoMessageComponent),
            new PropertyMetadata(0.0));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty IsReceivingProperty = DependencyProperty.Register(
            nameof(IsReceiving),
            typeof(bool),
            typeof(VideoMessageComponent),
            new PropertyMetadata(false));

        public bool IsReceiving
        {
            get => (bool)GetValue(IsReceivingProperty);
            set => SetValue(IsReceivingProperty, value);
        }

        public static readonly DependencyProperty IsSendingProperty = DependencyProperty.Register(
            nameof(IsSending),
            typeof(bool),
            typeof(VideoMessageComponent),
            new PropertyMetadata(false));

        public bool IsSending
        {
            get => (bool)GetValue(IsSendingProperty);
            set => SetValue(IsSendingProperty, value);
        }

        public static readonly DependencyProperty PlayVideoProperty =
            DependencyProperty.Register(
                nameof(PlayVideo),
                typeof(ICommand),
                typeof(VideoMessageComponent));

        public ICommand PlayVideo
        {
            get => (ICommand)GetValue(PlayVideoProperty);
            set => SetValue(PlayVideoProperty, value);
        }

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(
                nameof(FileName),
                typeof(string),
                typeof(VideoMessageComponent),
                new PropertyMetadata(""));

        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }
    }
}