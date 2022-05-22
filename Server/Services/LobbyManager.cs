using Draw.Rodeo.Shared.Data;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Draw.Rodeo.Server.Services
{
    public class LobbyManager
    {
        //Key ist LobbyID
        private ConcurrentDictionary<string, LobbyInfo> _Lobbies { get; set; }

        private const string _LobbyIDChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly Random _RandomGen = new();

        public LobbyManager()
        {
            _Lobbies = new();
        }


#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async Task NewFromConnection(string connectionID)
        {
            string lobbyID = GenerateNewLobbyID();
            LobbyInfo lobbyInfo = new();
            lobbyInfo.Connections.Add(connectionID);
            lobbyInfo.CurrentDrawer = connectionID;
            
            await AddOrUpdate(lobbyID, lobbyInfo);
            ResetAuthorizedConnections(lobbyID);
        }

        public async Task AddOrUpdate(string lobbyID, LobbyInfo lobbyInfo)

        {
            if (IsIllegalID(lobbyID))
                return;

            _Lobbies[lobbyID] = lobbyInfo;
        }

        public async Task Remove(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies.TryRemove(lobbyID, out LobbyInfo? lobbyInfo);
        }

        public async Task RemovePlayer(string connectionID)
        {
            LobbyInfo? lobby = _Lobbies.First(lobby => lobby.Value.Connections.Contains(connectionID)).Value;

            if (lobby.ConnectionsThatHaveGuessedCorrectly.Contains(connectionID))
                lobby.ConnectionsThatHaveGuessedCorrectly.Remove(connectionID);

            lobby.Connections.Remove(connectionID);
        }

        public async Task SetRoundInfo(string lobbyID, RoundInfo roundInfo)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].Rounds.CurrentRound = 0;
            _Lobbies[lobbyID].Rounds.TotalRounds = roundInfo.TotalRounds;
            _Lobbies[lobbyID].Rounds.TurnDuration = roundInfo.TurnDuration;
        }

        public async Task StartGame(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].Rounds.CurrentRound = 1;
            _Lobbies[lobbyID].CurrentDrawer = _Lobbies[lobbyID].Connections.First();

            ResetDrawingState(lobbyID);
            ResetAuthorizedConnections(lobbyID);
        }

        public async Task StopGame(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].Rounds.CurrentRound = 0;
            _Lobbies[lobbyID].CurrentDrawer = _Lobbies[lobbyID].Connections.First();
            ResetDrawingState(lobbyID);
            ResetAuthorizedConnections(lobbyID);
        }

        public async Task NextTurn(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].CurrentDrawing = _Lobbies[lobbyID].Connections
                                                                .SkipWhile(id => id != _Lobbies[lobbyID].CurrentDrawing)
                                                                .Skip(1)
                                                                .First();
            ResetDrawingState(lobbyID);
            ResetAuthorizedConnections(lobbyID);
        }

        public async Task NextRound(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].Rounds.CurrentRound++;
            _Lobbies[lobbyID].CurrentDrawer = _Lobbies[lobbyID].Connections.First();
            ResetDrawingState(lobbyID);
            ResetAuthorizedConnections(lobbyID);
        }

        public async Task SetCurrentWord(string lobbyID, string word)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].CurrentWord = word;
            _Lobbies[lobbyID].DisplayedWord = Regex.Replace(_Lobbies[lobbyID].CurrentWord, "[A-Za-z]", "_");
        }

        public async Task SetNextLobbyLeader(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].CurrentDrawer = _Lobbies[lobbyID].Connections.First();
        }

        public async Task AddPlayer(string connectionID, string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return;

            _Lobbies[lobbyID].Connections.Add(connectionID);
        }

        public async Task AddConnectionAsAuth(string connectionID)
        {
            LobbyInfo lobby = _Lobbies.First(info => info.Value.Connections.Contains(connectionID)).Value;
            lobby.ConnectionsThatHaveGuessedCorrectly.Add(connectionID);
        }

        public async Task<bool> LobbyExist(string lobbyID)
        {
            return DoesLobbyExist(lobbyID);
        }

        public async Task<bool> IsGameActive(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return false;

            return _Lobbies[lobbyID].Rounds.CurrentRound != 0;
        }

        public async Task<bool> HasAnsweredCorrectly(string connectionID)
        {
            return _Lobbies.First(info => info.Value.Connections.Contains(connectionID))
                           .Value
                           .ConnectionsThatHaveGuessedCorrectly
                           .Contains(connectionID);
        }

        public async Task<bool> IsLastInTurnOrder(string connectionID)
        {
            string lastConnection = _Lobbies.First(info => info.Value.Connections.Contains(connectionID))
                                            .Value
                                            .Connections
                                            .Last();
            return lastConnection == connectionID;
        }

        public async Task<bool> IsFinalRound(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return false;

            return _Lobbies[lobbyID].Rounds.CurrentRound == _Lobbies[lobbyID].Rounds.TotalRounds;
        }

        public async Task<bool> IsOnLastPlayer(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return false;

            return _Lobbies[lobbyID].Connections.Count == 1;
        }

        public async Task<int> GetCorrectGuessesCount(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return 1;

            return _Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly.Count;
        }

        public async Task<int> GetGuessPercentage(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return 100;

            return (100 * _Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly.Count) / _Lobbies[lobbyID].Connections.Count;
        }

        public async Task<string> GetCurrentDrawer(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return string.Empty;

            return _Lobbies[lobbyID].CurrentDrawer;
        }

        public async Task<string> GetCurrentWord(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return string.Empty;

            return _Lobbies[lobbyID].CurrentWord;
        }

        public async Task<string> GetDisplayedWord(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return string.Empty;

            return _Lobbies[lobbyID].DisplayedWord;
        }

        public async Task<string> ShowNextDisplayNameChar(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return string.Empty;

            char[] displayedWord = _Lobbies[lobbyID].DisplayedWord.ToCharArray();
            int index = _RandomGen.Next(displayedWord.Length);

            while(displayedWord[index] != '_')
            {
                index = _RandomGen.Next(displayedWord.Length);
            }

            displayedWord[index] = _Lobbies[lobbyID].CurrentWord[index];

            _Lobbies[lobbyID].DisplayedWord = new string(displayedWord);

            return _Lobbies[lobbyID].DisplayedWord;
        }

        public async Task<string> GetLobbyID(string connectionID)
        {
            if(!_Lobbies.Any(info => info.Value.Connections.Contains(connectionID)))
                return string.Empty;

            return _Lobbies.First(info => info.Value.Connections.Contains(connectionID)).Key;
        }

        public async Task<List<string>> GetAllLobbyConnections(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return new();

            return _Lobbies[lobbyID].Connections.ToList();
        }

        public async Task<List<string>> GetAuthorizedConnections(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return new();

            return _Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly.ToList();
        }

        public async Task<List<string>> GetUnauthorizedConnections(string lobbyID)
        {
            if (IsIllegalID(lobbyID) || !DoesLobbyExist(lobbyID))
                return new();

            return _Lobbies[lobbyID].Connections
                                    .Except(_Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly)
                                    .ToList();
        }

#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        private void ResetAuthorizedConnections(string lobbyID)
        {
            _Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly.Clear();
            _Lobbies[lobbyID].ConnectionsThatHaveGuessedCorrectly.Add(_Lobbies[lobbyID].CurrentDrawer);
        }

        private void ResetDrawingState(string lobbyID)
        {
            _Lobbies[lobbyID].CurrentDrawing = string.Empty;
            _Lobbies[lobbyID].CurrentWord = string.Empty;
        }

        private bool IsIllegalID(string lobbyID)
        {
            return string.IsNullOrWhiteSpace(lobbyID) || lobbyID.Length != 5;
        }

        private bool DoesLobbyExist(string lobbyID)
        {
            return _Lobbies.ContainsKey(lobbyID);
        }

        private string GenerateNewLobbyID()
        {
            string lobbyID = string.Empty;

            while(IsIllegalID(lobbyID) || DoesLobbyExist(lobbyID))
            {
                //Generiere 5-Zeichen-lange ID
                //Zeichen: A-Z und 0-9
                lobbyID = new string(Enumerable.Repeat(_LobbyIDChars, 5)
                                               .Select(s => s[_RandomGen.Next(s.Length)])
                                               .ToArray());
            }

            return lobbyID;
        }

    }
}
