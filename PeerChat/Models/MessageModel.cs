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

        public DateTime TimeStamp { get; }

        public MessageType Type { get; }
        public MessageDirection Direction { get; }


        public MessageModel(MessageType type, MessageDirection direction)
        {
            Type = type;
            Direction = direction;
            TimeStamp = DateTime.Now;
        }
    }
}