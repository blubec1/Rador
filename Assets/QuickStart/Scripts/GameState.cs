using System;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

//SERVER-ONLY
public class GameState : NetworkBehaviour
{
    public enum ROUND_STATUS
    {
        ONGOING,
        ENDING,
        SETUP
    }

    public enum GAMEMODE
    {
        DEATHMATCH,
        TEAMDEATHMATCH,
    }
    public ROUND_STATUS currentRoundStatus;
    public GAMEMODE currentGameMode = GAMEMODE.DEATHMATCH;
    public int maxFragsDeathmatch;
    public int maxFragsTeamdeathmatch;
    public int DownTime;
    int votesDM, votesTDM, DMTeamInteger, team1Frags, team2Frags, team1Count, team2Count;

    void OnEnable()
    {
        PlayerGeneral.OnDeath += OnDeath;
        NetworkManagerScript.OnSVClientConnected += OnFreshSpawn;
    }

    void OnDisable()
    {
        PlayerGeneral.OnDeath -= OnDeath;
        NetworkManagerScript.OnSVClientConnected -= OnFreshSpawn;
    }

    public static event Action OnRoundStart;

    public void StartRound()
    {
        currentRoundStatus = ROUND_STATUS.ONGOING;

        if (votesDM >= votesTDM)
        {
            currentGameMode = GAMEMODE.DEATHMATCH;
        }
        else
        {
            currentGameMode = GAMEMODE.TEAMDEATHMATCH;
        }

        //punerea playerilor in echipe diferite in caz de Deathmatch si in 2 echipe diferite in caz de TeamDeathmatch
        if (currentGameMode == GAMEMODE.DEATHMATCH)
        {
            DMTeamInteger = 1;
            foreach (var conn in NetworkServer.connections)
            {
                conn.Value.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = DMTeamInteger;
                DMTeamInteger++;
            }
        }
        else if (currentGameMode == GAMEMODE.TEAMDEATHMATCH)
        {
            int playerCount = NetworkServer.connections.Count;
            team1Count = playerCount / 2;
            team2Count = playerCount - team1Count;
            int auxt1 = team1Count;
            int auxt2 = team2Count;
            foreach (var conn in NetworkServer.connections)
            {
                if (auxt1 != 0 && auxt2 != 0)
                {
                    int randomTeam = UnityEngine.Random.Range(1, 2);
                    conn.Value.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = randomTeam;
                    if (randomTeam == 1)
                        auxt1--;
                    else
                        auxt2--;
                }
                else
                {
                    if (auxt1 != 0)
                    {
                        conn.Value.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = 1;
                        auxt1--;
                    }
                    else
                    {
                        conn.Value.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = 2;
                        auxt2--;
                    }
                }
            }
        }

        //reseateaza fiecare player in parte si il spawneaza
        Dictionary<int, NetworkConnectionToClient> conns = NetworkServer.connections;
        foreach (var conn in conns)
        {
            PlayerGeneral PG = conn.Value.identity.GetComponent<PlayerGeneral>();
            PG.ChangeFrags(-PG.GetFrags());
            PG.ChangeDeaths(-PG.GetDeaths());
            PG.SvStartRound();
            OnRoundStart?.Invoke();
        }
    }
    //omoara fiecare player si ii aplica UI-ul de voting
    public void EndRound(string winnerInfo)
    {
        currentRoundStatus = ROUND_STATUS.ENDING;
        Dictionary<int, NetworkConnectionToClient> conns = NetworkServer.connections;
        foreach (var conn in conns)
        {
            PlayerGeneral PG = conn.Value.identity.GetComponent<PlayerGeneral>();
            string winnerInfoFwd = winnerInfo;
            PG.SvEndRound(winnerInfoFwd);
        }
        Invoke(nameof(StartRound), DownTime);
        votesDM = 0;
        votesTDM = 0;
        team1Frags = 0;
        team2Frags = 0;
    }
    public void OnDeath(PlayerGeneral victimI, PlayerGeneral killerI)
    {
        if (killerI != null)
            switch (currentGameMode)
            {
                case GAMEMODE.DEATHMATCH:
                    OnDeathDeathmatch(victimI, killerI);
                    break;
                case GAMEMODE.TEAMDEATHMATCH:
                    OnDeathTeamDeathMatch(victimI, killerI);
                    break;
            }
    }
    //da o echipa in functie de gamemode playerului nou conectat
    public void OnFreshSpawn(NetworkConnectionToClient conn)
    {
        if (currentGameMode == GAMEMODE.DEATHMATCH)
        {
            conn.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = DMTeamInteger;
            DMTeamInteger++;
        }
        else
        {
            if (team1Count > team2Count)
            {
                conn.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = 2;
                team2Count++;
            }
            else
            {
                conn.identity.gameObject.GetComponent<PlayerGeneral>().TeamID = 1;
                team1Count++;
            }
        }

    }

    public void OnDeathDeathmatch(PlayerGeneral victimI, PlayerGeneral killerI)
    {
        if (killerI.GetFrags() >= maxFragsDeathmatch)
        {
            EndRound(killerI.ps.name);
        }
    }

    public void OnDeathTeamDeathMatch(PlayerGeneral victimI, PlayerGeneral killerI)
    {

        if (killerI.TeamID == 1)
        {
            team1Frags++;
            if (team1Frags == maxFragsTeamdeathmatch)
                EndRound("Team 1");
        }
        else if (killerI.TeamID == 2)
        {
            team2Frags++;
            if (team2Frags == maxFragsTeamdeathmatch)
                EndRound("Team 2");
        }
    }

    public void VoteDeathmatch()
    {
        votesDM++;
    }

    public void VoteTeamDeathmatch()
    {
        votesTDM++;
    }
    
}
