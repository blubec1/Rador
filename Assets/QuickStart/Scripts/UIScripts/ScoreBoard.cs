using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ScoreBoard : NetworkBehaviour
{

    Dictionary<int, NetworkConnectionToClient> conns;
    public RectTransform panelTransform;
    public GameObject scorePrefab;
    public const float timeToWait = 0.1f;
    Dictionary<uint, GameObject> PlayerLabels = new();
    /// eventurile necesare pentru a tine scoreboard-ul la curent
    public void Start()
    {
        NetworkManagerScript.OnSVClientConnected += SvUpdateConnect;
        NetworkManagerScript.OnNewClientDisconnected += SvUpdateDisconnect;
        PlayerGeneral.OnDeath += SvUpdateValues;
        GameState.OnRoundStart += SvRoundStart;
    }

    void OnDisable()
    {
        NetworkManagerScript.OnSVClientConnected -= SvUpdateConnect;
        NetworkManagerScript.OnNewClientDisconnected -= SvUpdateDisconnect;
        PlayerGeneral.OnDeath -= SvUpdateValues;
        GameState.OnRoundStart -= SvRoundStart;
    }

    //cand se conecteaza playerul, da-i toate informatiile asupra celorlalti jucatori mai putin el, apoi adauga in scoreboard pe playerul nou conectat
    [Server]
    void SvUpdateConnect(NetworkConnectionToClient conn)
    {
        PlayerGeneral PG;
        foreach (var connec in NetworkServer.connections)
        {
            if (connec.Value != conn)
            {
                PG = connec.Value.identity.gameObject.GetComponent<PlayerGeneral>();
                TargetAddPlayer(conn, connec.Value.identity.netId, PG.ps.playerName, PG.GetFrags(), PG.GetDeaths());
            }
        }

        StartCoroutine(WaitToAddPlayer(conn.identity, timeToWait));
    }

    //playerul iese, scoate-l din scoreboard
    [Server]
    void SvUpdateDisconnect(NetworkConnectionToClient conn)
    {
        DeleteDisconnectedClientRpc(conn.identity.netId);
    }

    //activat oricand moare cineva
    [Server]
    void SvUpdateValues(PlayerGeneral victim, PlayerGeneral killer)
    {
        if (killer != null)
            UpdateValuesOnDeathRpc(victim.netId, victim.GetDeaths(), killer.netId, killer.GetFrags());
        else
            UpdateValuesOnDeathRpc(victim.netId, victim.GetDeaths(), 100, 0);
    }

    [Server]
    void SvRoundStart()
    {
        ResetValuesRpc();
    }

    //adauga playerul respectiv tuturor clientilor
    [ClientRpc]
    void AddPlayerRpc(uint connID, string playerName, int frags, int deaths)
    {
        GameObject playerScore = Instantiate(scorePrefab, panelTransform);
        ScorePlayerStatus playerStatus = playerScore.GetComponent<ScorePlayerStatus>();

        RectTransform rectTransform = playerScore.GetComponent<RectTransform>();

        rectTransform.anchorMin = new(0, 1);
        rectTransform.anchorMax = new(0, 1);

        playerStatus.PlayerName.text = playerName;
        playerStatus.PlayerFrags.text = frags.ToString();
        playerStatus.PlayerDeaths.text = deaths.ToString();

        PlayerLabels.Add(connID, playerScore);
    }

    //adauga playerul respectiv numai unui singur client caruia i se atribuie primul parametru "conn"
    [TargetRpc]
    void TargetAddPlayer(NetworkConnectionToClient conn, uint connId, string playerName, int frags, int deaths)
    {
        GameObject playerScore = Instantiate(scorePrefab, panelTransform);
        ScorePlayerStatus playerStatus = playerScore.GetComponent<ScorePlayerStatus>();

        RectTransform rectTransform = playerScore.GetComponent<RectTransform>();

        rectTransform.anchorMin = new(0, 1);
        rectTransform.anchorMax = new(0, 1);

        playerStatus.PlayerName.text = playerName;
        playerStatus.PlayerFrags.text = frags.ToString();
        playerStatus.PlayerDeaths.text = deaths.ToString();
        PlayerLabels.Add(connId, playerScore);
    }

    [ClientRpc]
    void UpdateValuesOnDeathRpc(uint victimId, int updatedDeaths, uint killerId, int updatedFrags)
    {
        ScorePlayerStatus playerStatus = PlayerLabels[victimId].GetComponent<ScorePlayerStatus>();
        playerStatus.PlayerDeaths.text = updatedDeaths.ToString();
        if (killerId != 100)
        {
            playerStatus = PlayerLabels[killerId].GetComponent<ScorePlayerStatus>();
            playerStatus.PlayerFrags.text = updatedFrags.ToString();
        }
    }

    [ClientRpc]
    void ResetValuesRpc()
    {
        foreach (var playerLabel in PlayerLabels)
        {
            ScorePlayerStatus playerStatus = playerLabel.Value.GetComponent<ScorePlayerStatus>();
            playerStatus.PlayerFrags.text = "0";
            playerStatus.PlayerDeaths.text = "0";
        }
    }


    [ClientRpc]
    void DeleteDisconnectedClientRpc(uint connId)
    {
        Destroy(PlayerLabels[connId]);
        PlayerLabels.Remove(connId);
    }

    //functie posibil redundanta, lasata pentru siguranta in caz ca playerul nu se initializeaza in timp util
    IEnumerator WaitToAddPlayer(NetworkIdentity conn, float time)
    {
        yield return new WaitForSeconds(time);
        PlayerGeneral PG = conn.gameObject.GetComponent<PlayerGeneral>();
        AddPlayerRpc(conn.netId, PG.ps.playerName, PG.GetFrags(), PG.GetDeaths());
    }

}
