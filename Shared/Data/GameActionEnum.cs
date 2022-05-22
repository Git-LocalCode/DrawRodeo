using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draw.Rodeo.Shared.Data
{
    public enum GameAction
    {
        Nothing,
        NewRoundOrTurn,
        NewLobbyLeader,
        LobbyRemoved,
        PlayerDisconnected,
        EndTurn
    }
}
