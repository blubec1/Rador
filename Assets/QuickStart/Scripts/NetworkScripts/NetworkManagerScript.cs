using UnityEngine;
using Mirror;
using Mirror.Examples.Basic;
using System;
using Telepathy;

//da override unor functii din componenta de NetworkManager pentru a adauga eventuri si anumite cazuri
public class NetworkManagerScript : NetworkManager
{

    public int maxPlayers;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnectionToClient> OnSVClientConnected;
    public static event Action<NetworkConnectionToClient> OnNewClientDisconnected;

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        OnClientConnected?.Invoke();

    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        OnClientDisconnected?.Invoke();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        OnSVClientConnected?.Invoke(conn);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        PlayerGeneral PI = conn.identity.GetComponent<PlayerGeneral>();
        PI.ps.HandleDisconnect();

        OnNewClientDisconnected?.Invoke(conn);

        base.OnServerDisconnect(conn);

    }

}
