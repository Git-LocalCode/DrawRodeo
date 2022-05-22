namespace Draw.Rodeo.Shared.Data
{
    public class MessageInfo
    {
        public string Message { get; set; }
        public PlayerInfo Player { get; set; }

        public MessageInfo()
        {
            Message = string.Empty;
            Player = new();
        }
    }
}
