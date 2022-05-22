using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draw.Rodeo.Shared.Data
{
    public class ScoreInfo
    {
        public int Score { get; set; }
        public int TurnScore { get; set; }
        public PlayerInfo Player { get; set; }

        public ScoreInfo()
        {
            Score = 0;
            TurnScore = 0;
            Player = new();
        }
    }
}
