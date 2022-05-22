namespace Draw.Rodeo.Shared.Data
{
    public class RoundInfo
    {
        public int CurrentRound { get; set; }
        public int TotalRounds { get; set; }
        public int TurnDuration { get; set; }

        public RoundInfo()
        {
            CurrentRound = 0;
            TotalRounds = 0;
            TurnDuration = 0;
        }
    }
}
