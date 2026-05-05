namespace PeerChat.Enums
{
    public enum MessageType : byte
    {
        Text = 0x01,
        Image = 0x02,
        Video = 0x03,
        Typing = 0x04,
        Disconnect = 0x05
    }
}