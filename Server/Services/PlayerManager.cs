using Draw.Rodeo.Shared.Data;
using System.Collections.Concurrent;

namespace Draw.Rodeo.Server.Services
{
    public class PlayerManager
    {
        //Key ist ConnectionID
        private ConcurrentDictionary<string, ScoreInfo> _Players { get; set; }

        public PlayerManager()
        {
            _Players = new();
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async Task New(string connectionID)
        {
            await AddOrUpdate(connectionID, new());
        }

        public async Task AddOrUpdate(string connectionID, ScoreInfo playerInfo)

        {
            _Players[connectionID] = playerInfo;
        }

        public async Task Remove(string connectionID)
        {
            _Players.TryRemove(connectionID, out ScoreInfo? playerInfo);
        }

        public async Task ChangeName(string connectionID, string name)
        {
            _Players[connectionID].Player.Name = name;
        }
        
        public async Task ChangeAvatar(string connectionID, string avatar)
        {
            _Players[connectionID].Player.Avatar = avatar;
        }

        public async Task ChangePlayerInfo(string connectionID, PlayerInfo playerInfo)
        {
            _Players[connectionID].Player = playerInfo;
        }

        public async Task ResetScore(string connectionID)
        {
            _Players[connectionID].Score = 0;
        }

        public async Task IncreaseScore(string connectionID)
        {
            _Players[connectionID].Score += _Players[connectionID].TurnScore;
            _Players[connectionID].TurnScore = 0;
        }

        public async Task SetTurnScore(string connectionID, int score)
        {
            _Players[connectionID].TurnScore = score;
        }

        public async Task IncreaseTurnScoreGuesser(string connectionID, int guessPercentage)
        {
            _Players[connectionID].TurnScore += (105 - guessPercentage);
        }

        public async Task SetTurnScoreDrawer(string connectionID, int guessPercentage)
        {
            _Players[connectionID].TurnScore = guessPercentage;
        }

        public async Task<int> GetScore(string connectionID)
        {
            return _Players[connectionID].Score;
        }

        public async Task<PlayerInfo> GetPlayerInfo(string connectionID)
        {
            return _Players[connectionID].Player;
        }

        public async Task<ScoreInfo> GetScoreInfo(string connectionID)
        {
            return _Players[connectionID];
        }

        public async Task<List<ScoreInfo>> GetScoreInfos(List<string> connectionIDs)
        {
            List<ScoreInfo> scoreInfos = new List<ScoreInfo>();

            foreach (string connectionID in connectionIDs)
                scoreInfos.Add(_Players[connectionID]);

            return scoreInfos;
        }

#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

    }
}
