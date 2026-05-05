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
using System.Diagnostics;
using PeerChat.Models;

namespace PeerChat.View
{
    /// <summary>
    /// Interaction logic for ChatView.xaml
    /// </summary>
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        private void ImageClick(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            if (image?.Source != null)
            {
                var fullImageWindow = new Window
                {
                    Title = "Full Size Image",
                    Content = new Image
                    {
                        Source = image.Source,
                        Stretch = Stretch.Uniform,
                        MaxHeight = 400,
                        MaxWidth = 400
                    },
                    Background = Brushes.Black,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };

                fullImageWindow.ShowDialog();
            }
        }
    }
}