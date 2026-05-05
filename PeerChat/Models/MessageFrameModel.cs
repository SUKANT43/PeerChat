using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerChat.models
{
    public class MessageFrameModel
    {
        public byte Type { get; set; }
        public byte[] Payload { get; set; }
    }
}
