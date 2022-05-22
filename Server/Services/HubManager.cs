using Draw.Rodeo.Shared.Data;

namespace Draw.Rodeo.Server.Services
{
    public class HubManager
    {
        private LobbyManager _LM;
        private PlayerManager _PM;

        public HubManager(LobbyManager lobbyManager, PlayerManager playerManager)
        {
            _LM = lobbyManager;
            _PM = playerManager;
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async Task RegisterNewConnection(string connectionID)
        {
            await _PM.New(connectionID);
        }

        public async Task CreateNewLobby(string connectionID)
        {
            await _LM.NewFromConnection(connectionID);
        }

        public async Task SetPlayerInfo(string connectionID, PlayerInfo playerInfo)
        {
            await _PM.ChangePlayerInfo(connectionID, playerInfo);
        }

        public async Task SetRoundInfo(string connectionID, RoundInfo roundInfo)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            await _LM.SetRoundInfo(lobbyID, roundInfo);
        }

        public async Task StartGame(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            await _LM.StartGame(lobbyID);
        }

        public async Task StartNextTurn(string lobbyID)
        {
            await _LM.NextTurn(lobbyID);

            List<string> connections = await _LM.GetAllLobbyConnections(lobbyID);
            foreach(string connection in connections)
                await _PM.IncreaseScore(connection);
        }

        public async Task StartNextRound(string lobbyID)
        {
            await _LM.NextRound(lobbyID);
            List<string> connections = await _LM.GetAllLobbyConnections(lobbyID);
            foreach (string connection in connections)
                await _PM.IncreaseScore(connection);
        }

        public async Task CalcFinalScore(string lobbyID)
        {
            List<string> connections = await _LM.GetAllLobbyConnections(lobbyID);
            foreach (string connection in connections)
                await _PM.IncreaseScore(connection);
        }

        public async Task GuesserIsCorrect(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            int guessPercent = await _LM.GetGuessPercentage(lobbyID);
            await _PM.IncreaseTurnScoreGuesser(connectionID, guessPercent);
            await _LM.AddConnectionAsAuth(connectionID);
        }

        public async Task EndTurn(string lobbyID)
        {
            string drawer = await _LM.GetCurrentDrawer(lobbyID);
            int guessPercent = await _LM.GetGuessPercentage(lobbyID);
            var authCon = await _LM.GetAuthorizedConnections(lobbyID);
            if (authCon.Count == 1)
                return;

            await _PM.SetTurnScoreDrawer(drawer, guessPercent);
        }

        public async Task EndGame(string lobbyID)
        {
            await _LM.StopGame(lobbyID);

            List<string> connections = await _LM.GetAllLobbyConnections(lobbyID);
            foreach(string connection in connections)
                await _PM.ResetScore(connection);
        }

        public async Task<bool> IsLastRound(string lobbyID)
        {
            return await _LM.IsFinalRound(lobbyID);
        }

        public async Task<bool> ConnectPlayer(string connectionID, string lobbyID)
        {
            if (!await _LM.LobbyExist(lobbyID))
                return false;

            await _LM.AddPlayer(connectionID, lobbyID);
            return true;
        }

        public async Task<bool> IsPlayerAuthorized(string connectionID)
        {
            return await _LM.HasAnsweredCorrectly(connectionID);
        }

        public async Task<bool> IsCorrectWord(string connectionID, string word)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            word = word.ToUpper();
            return word == await _LM.GetCurrentWord(lobbyID);
        }

        public async Task<bool> IsGameActive(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            return await _LM.IsGameActive(lobbyID);
        }

        public async Task<bool> IsLastInRound(string connectionID)
        {
            return await _LM.IsLastInTurnOrder(connectionID);
        }

        public async Task<int> GetTurnDuration(string lobbyID)
        {
            return await _LM.GetTurnDuration(lobbyID);
        }

        public async Task<string> GetDrawerConnection(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            return await _LM.GetCurrentDrawer(lobbyID);
        }

        public async Task<string> GetDrawerConnectionByLobbyID(string lobbyID)
        {
            return await _LM.GetCurrentDrawer(lobbyID);
        }

        public async Task<string> GetNewDisplayName(string connectionID, string wordChoice)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            await _LM.SetCurrentWord(lobbyID, wordChoice);
            return await _LM.GetDisplayedWord(lobbyID);
        }

        public async Task<string> GetLobbyID(string connectionID)
        {
            return await _LM.GetLobbyID(connectionID);
        }

        public async Task<string> GetCurrentWord(string lobbyID)
        {
            return await _LM.GetCurrentWord(lobbyID);
        }

        public async Task<string> GetDisplayWord(string lobbyID)
        {
            return await _LM.GetDisplayedWord(lobbyID);
        }

        /// <summary>
        /// Entferne die Verbindungs-ID aus allen Liste
        /// </summary>
        /// <param name="connectionID"></param>
        /// <returns>Action Needed</returns>
        public async Task<GameAction> RemoveConnection(string connectionID)
        {
            await _PM.Remove(connectionID);
            string lobbyID = await _LM.GetLobbyID(connectionID);

            if (string.IsNullOrEmpty(lobbyID))
                return GameAction.Nothing;

            if (await _LM.IsOnLastPlayer(lobbyID))
            {
                await _LM.Remove(lobbyID);
                return GameAction.LobbyRemoved;
            }

            if (await _LM.GetCurrentDrawer(lobbyID) == connectionID && await _LM.IsGameActive(lobbyID))
            {
                if (await _LM.IsLastInTurnOrder(connectionID))
                    await _LM.NextRound(lobbyID);
                else
                    await _LM.NextTurn(lobbyID);

                List<string> connections = await _LM.GetAllLobbyConnections(lobbyID);
                foreach (string connection in connections)
                    await _PM.SetTurnScore(connection, 0);

                await _LM.RemovePlayer(connectionID);
                return GameAction.NewRoundOrTurn;
            }

            if (await _LM.GetCurrentDrawer(lobbyID) == connectionID && !await _LM.IsGameActive(lobbyID))
            {
                await _LM.RemovePlayer(connectionID);
                await _LM.SetNextLobbyLeader(lobbyID);
                return GameAction.NewLobbyLeader;
            }

            //var guessers = await _LM.GetUnauthorizedConnections(lobbyID);
            //if(guessers.All(x => x == connectionID))
            //{
            //    //TODO next turn but what if last 
            //    await _LM.RemovePlayer(connectionID);
            //    return GameAction.EndTurn;
            //}

            await _LM.RemovePlayer(connectionID);
            return GameAction.PlayerDisconnected;
        }

        public async Task<PlayerInfo> GetPlayerInfo(string connectionID)
        {
            return await _PM.GetPlayerInfo(connectionID);
        }

        public async Task<ScoreInfo> GetScoreInfo(string connectionID)
        {
            return await _PM.GetScoreInfo(connectionID);
        }

        public async Task<List<ScoreInfo>> GetPlayerList(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            List<string>? connections = await _LM.GetAllLobbyConnections(lobbyID);

            return await _PM.GetScoreInfos(connections);
        }

        public async Task<List<string>> GetLobbyConnections(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            return await _LM.GetAllLobbyConnections(lobbyID);
        }

        public async Task<List<string>> GetLobbyConnectionsByLobbyID(string lobbyID)
        {
            return await _LM.GetAllLobbyConnections(lobbyID);
        }

        public async Task<List<string>> GetGuesserConnections(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            List<string>? connections = await _LM.GetAllLobbyConnections(lobbyID);
            connections.Remove(await _LM.GetCurrentDrawer(lobbyID));
            return connections;
        }

        public async Task<List<string>> GetGuesserConnectionsByLobbyID(string lobbyID)
        {
            List<string>? connections = await _LM.GetAllLobbyConnections(lobbyID);
            connections.Remove(await _LM.GetCurrentDrawer(lobbyID));
            return connections;
        }

        public async Task<List<string>> GetAuthConnections(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            return await _LM.GetAuthorizedConnections(lobbyID);
        }

        public async Task<List<string>> GetAuthConnectionsByLobbyID(string lobbyID)
        {
            return await _LM.GetAuthorizedConnections(lobbyID);
        }

        public async Task<List<string>> GetUnauthConnections(string connectionID)
        {
            string lobbyID = await _LM.GetLobbyID(connectionID);
            return await _LM.GetUnauthorizedConnections(lobbyID);
        }

        public async Task<List<string>> GetUnauthConnectionsByLobbyID(string lobbyID)
        {
            return await _LM.GetUnauthorizedConnections(lobbyID);
        }

        public async Task<string> NextDisplay(string lobbyID)
        {
            return await _LM.ShowNextDisplayNameChar(lobbyID);
        }

#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

    }
}
