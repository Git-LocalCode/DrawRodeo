namespace Draw.Rodeo.Shared.Data
{
    public class ScoreCardInfo
    {
        public bool IsCurrentDrawer { get; set; }
        public bool IsCurrentPlayer { get; set; }
        public bool HasGuessedCorrectly { get; set; }
        public ScoreInfo Score { get; set; }

        public ScoreCardInfo()
        {
            IsCurrentDrawer = false;
            IsCurrentPlayer = false;
            HasGuessedCorrectly = false;
            Score = new();
        }
    }
}
