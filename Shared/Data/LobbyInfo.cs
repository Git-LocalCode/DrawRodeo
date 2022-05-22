namespace Draw.Rodeo.Shared.Data
{
    public class LobbyInfo
    {
        public string CurrentDrawer { get; set; }
        public string CurrentWord { get; set; }
        public string DisplayedWord { get; set; }
        public RoundInfo Rounds { get; set; }
        public List<string> Connections { get; set; }
        public List<string> ConnectionsThatHaveGuessedCorrectly { get; set; }

        public LobbyInfo()
        {
            CurrentDrawer = string.Empty;
            CurrentWord = string.Empty;
            DisplayedWord = string.Empty;
            Rounds = new();
            Connections = new();
            ConnectionsThatHaveGuessedCorrectly = new();
        }
    }
}
