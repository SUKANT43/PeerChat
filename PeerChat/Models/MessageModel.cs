using PeerChat.Base;
using PeerChat.Enums;
using System;
using System.Windows.Media.Imaging;

namespace PeerChat.Models
{
    public class MessageModel : Observable
    {

        public string Content{ get; set; }
        public BitmapImage ImageData{ get; set; }
        public string FileName{ get; set; }
        public string FilePath { get; set; }

        public String TimeStamp { get; }
        public MessageType Type { get; }
        public MessageDirection Direction { get; }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private bool _isReceiving;
        public bool IsReceiving
        {
            get => _isReceiving;
            set
            {
                _isReceiving = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage VideoThumbnail { get; set; }


        public MessageModel(MessageType type, MessageDirection direction)
        {
            Type = type;
            Direction = direction;
            TimeStamp = DateTime.Now.ToString("HH:mm");
            Progress = 0;
            IsReceiving = false;
        }
    }
}