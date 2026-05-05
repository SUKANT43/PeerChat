using PeerChat.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerChat.Models
{
    public class DebugLogModel
    {
        public MessageDirection Direction { get; }
        public MessageType Type { get; }
        public long PayloadSize { get; }
        public DateTime Timestamp { get; }

        public DebugLogModel(MessageDirection direction, MessageType type, long payloadSize)
        {
            Direction = direction;
            Type = type;
            PayloadSize = payloadSize;
            Timestamp = DateTime.Now;
        }
    }
}
