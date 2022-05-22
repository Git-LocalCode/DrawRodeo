using Draw.Rodeo.Server.Services;
using Draw.Rodeo.Shared.Data;
using Microsoft.AspNetCore.SignalR;

namespace Draw.Rodeo.Server.Hubs
{
    public class LobbyHub : Hub
    {
        private HubManager _Hub;
        private WordManager _Word;

        public LobbyHub(HubManager hubManager, WordManager wordManager)
        {
            _Hub = hubManager;
            _Word = wordManager;
        }

        //TODO End of Game

        public override async Task OnConnectedAsync()
        {
            await _Hub.RegisterNewConnection(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string lobbyID = await _Hub.GetLobbyID(Context.ConnectionId);
            PlayerInfo playerInfo = await _Hub.GetPlayerInfo(Context.ConnectionId);
            GameAction action = await _Hub.RemoveConnection(Context.ConnectionId);

            switch(action)
            {
                case GameAction.NewRoundOrTurn:
                    await NotifyNewRoundOrTurn();
                    await NotifyPlayerDisconnected(lobbyID, playerInfo);
                    await NotifyPlayerListChanged(lobbyID);
                    break;
                case GameAction.PlayerDisconnected:
                    await NotifyPlayerDisconnected(lobbyID, playerInfo);
                    await NotifyPlayerListChanged(lobbyID);
                    break;
                case GameAction.NewLobbyLeader:
                    await NotifyNewManager(lobbyID);
                    await NotifyPlayerDisconnected(lobbyID, playerInfo);
                    await NotifyPlayerListChanged(lobbyID);
                    break;
            }
            //TODO what if last to answer

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SetPlayerInfo(PlayerInfo playerInfo)
        {
            await _Hub.SetPlayerInfo(Context.ConnectionId, playerInfo);
        }

        public async Task SetWordChoice(string wordChoice)
        {
            string displayedWord = await _Hub.GetNewDisplayName(Context.ConnectionId, wordChoice);
            await NotifyDisplayWordChange(displayedWord, wordChoice);
        }

        public async Task StartGame(RoundInfo roundInfo)
        {
            await _Hub.SetRoundInfo(Context.ConnectionId, roundInfo);
            await _Hub.StartGame(Context.ConnectionId);
            await NotifyNewRoundOrTurn();
        }

        public async Task CreateLobby()
        {
            await _Hub.CreateNewLobby(Context.ConnectionId);
            string lobbyID = await _Hub.GetLobbyID(Context.ConnectionId);
            await Clients.Client(Context.ConnectionId).SendAsync("LobbyID", lobbyID);
            await NotifyNewManager(lobbyID);
            await NotifyPlayerConnected();
            await NotifyPlayerListChanged(lobbyID);
        }

        public async Task UpdateDrawing(string drawing)
        {
            List<string> guessers = await _Hub.GetGuesserConnections(Context.ConnectionId);
            foreach (string guesser in guessers)
                await Clients.Client(guesser).SendAsync("UpdateDrawing", drawing);
        }

        public async Task SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if(!await _Hub.IsGameActive(Context.ConnectionId))
            {
                await MessageAll(message);
                return;
            }

            if (await _Hub.IsPlayerAuthorized(Context.ConnectionId))
            {
                await MessageAuthConnections(message);
                return;
            }
                
            if (await _Hub.IsCorrectWord(Context.ConnectionId, message))
            {
                await _Hub.GuesserIsCorrect(Context.ConnectionId);
                string lobbyID = await _Hub.GetLobbyID(Context.ConnectionId);
                await NotifyPlayerListChanged(lobbyID);
                await Clients.Client(Context.ConnectionId).SendAsync("MessageFromSelf", message);
                await Clients.Client(Context.ConnectionId).SendAsync("GuessedCorrectly", message);

                //TODO what if they are the last to guess correctly
                return;
            }

            await MessageAll(message);
        }

        public async Task<bool> JoinLobby(string lobbyID)
        {
            bool connected = await _Hub.ConnectPlayer(Context.ConnectionId, lobbyID);

            if(!connected)
                return false;

            await Clients.Client(Context.ConnectionId).SendAsync("LobbyID", lobbyID);
            await Clients.Client(Context.ConnectionId).SendAsync("StartGuessing");

            await NotifyPlayerConnected();
            await NotifyPlayerListChanged(lobbyID);
            return true;
        }

        public async Task StartNextRoundOrTurnOrEndGame(string lobbyID)
        {

        }

        private async Task NotifyNewRoundOrTurn()
        {
            string drawer = await _Hub.GetDrawerConnection(Context.ConnectionId);
            List<string> wordChoice = await _Word.GetWords();
            await Clients.Client(drawer).SendAsync("StartDrawing", wordChoice);

            List<string>? guessers = await _Hub.GetGuesserConnections(Context.ConnectionId);
            foreach (string guesser in guessers)
                await Clients.Client(guesser).SendAsync("StartGuessing");
        }

        private async Task NotifyPlayerDisconnected(string lobbyID, PlayerInfo player)
        {
            List<string> connections = await _Hub.GetLobbyConnectionsByLobbyID(lobbyID);

            foreach (string connection in connections)
                await Clients.Client(connection).SendAsync("Disconnected", player);
        }

        private async Task NotifyPlayerConnected()
        {
            List<string> connections = await _Hub.GetLobbyConnections(Context.ConnectionId);
            PlayerInfo player = await _Hub.GetPlayerInfo(Context.ConnectionId);

            foreach (string connection in connections)
                await Clients.Client(connection).SendAsync("Connected", player);
        }

        private async Task NotifyNewManager(string lobbyID)
        {
            string drawer = await _Hub.GetDrawerConnectionByLobbyID(lobbyID);
            await Clients.Client(drawer).SendAsync("StartManaging");
        }

        private async Task NotifyPlayerListChanged(string lobbyID)
        {
            
            string drawerConnection = await _Hub.GetDrawerConnectionByLobbyID(lobbyID);
            List<string> authConnections = await _Hub.GetAuthConnectionsByLobbyID(lobbyID);
            List<string> unauthConnections = await _Hub.GetUnauthConnectionsByLobbyID(lobbyID);

            List<string> connections = await _Hub.GetLobbyConnectionsByLobbyID(lobbyID);
            foreach (string connection in connections)
            {
                List<ScoreCardInfo> scores = new();

                foreach (string authconnection in authConnections)
                {
                    scores.Add(new ScoreCardInfo()
                    {
                        HasGuessedCorrectly = true,
                        IsCurrentDrawer = (authconnection == drawerConnection),
                        IsCurrentPlayer = (authconnection == connection),
                        Score = await _Hub.GetScoreInfo(authconnection),
                    });
                }
                foreach (string unauthconnection in unauthConnections)
                {
                    scores.Add(new ScoreCardInfo()
                    {
                        HasGuessedCorrectly = false,
                        IsCurrentDrawer = (unauthconnection == drawerConnection),
                        IsCurrentPlayer = (unauthconnection == connection),
                        Score = await _Hub.GetScoreInfo(unauthconnection),
                    });
                }

                scores = scores.OrderBy(s => s.Score.Score).ToList();

                await Clients.Client(connection).SendAsync("UpdatePlayerList", scores);
            }
        }

        private async Task NotifyDisplayWordChange(string displayWord, string currentWord)
        {
            List<string> authConnections = await _Hub.GetAuthConnections(Context.ConnectionId);
            foreach (string connection in authConnections)
                await Clients.Clients(connection).SendAsync("UpdateDisplayWord", currentWord);

            List<string> unauthConnections = await _Hub.GetUnauthConnections(Context.ConnectionId);
            foreach (string connection in unauthConnections)
                await Clients.Client(connection).SendAsync("UpdateDisplayWord", displayWord);
        }

        private async Task MessageAuthConnections(string message)
        {
            PlayerInfo playerInfo = await _Hub.GetPlayerInfo(Context.ConnectionId);
            MessageInfo messageInfo = new MessageInfo() { Message = message, Player = playerInfo };
            List<string> authConnections = await _Hub.GetAuthConnections(Context.ConnectionId);
            authConnections.Remove(Context.ConnectionId);
            foreach(string connection in authConnections)
                await Clients.Client(connection).SendAsync("MessageFromOther", messageInfo);

            await Clients.Client(Context.ConnectionId).SendAsync("MessageFromSelf", messageInfo);
        }

        private async Task MessageAll(string message)
        {
            PlayerInfo playerInfo = await _Hub.GetPlayerInfo(Context.ConnectionId);
            MessageInfo messageInfo = new MessageInfo() { Message = message, Player = playerInfo };
            List<string> connections = await _Hub.GetLobbyConnections(Context.ConnectionId);
            connections.Remove(Context.ConnectionId);
            foreach (string connection in connections)
                await Clients.Client(connection).SendAsync("MessageFromOther", messageInfo);

            await Clients.Client(Context.ConnectionId).SendAsync("MessageFromSelf", messageInfo);
        }
    }
}
