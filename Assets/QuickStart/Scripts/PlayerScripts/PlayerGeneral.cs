using System;
using System.Data;
using Mirror;
using Mirror.Examples.Basic;
using Mirror.Examples.Common;
using QuickStart;
using UnityEngine;
using UnityEngine.UIElements;

//clasa cu scopul sa uneasca cele doua scripturi ale obiectului jucator intr-o singura clasa, adaugand si alte functii publice
public class PlayerGeneral : NetworkBehaviour
{
    public PlayerScript ps;
    public PlayerMovement pm;
    [SyncVar(hook = nameof(OnHPChange))]
    public float HPsynced;
    [SyncVar(hook = nameof(OnArmorChange))]
    public float ArmorSynced;
    //momentan armorPercentage este de 75
    public int maxArmor, maxHP, armorPercentage;
    [SyncVar(hook = nameof(OnLifeChange))]
    public bool isAlive;
    [SyncVar(hook = nameof(OnTeamChange))]
    public int TeamID;
    public int RespawnTime;
    public int RespawnInvulnTime;
    GameState gameState;


    public static Action<PlayerGeneral, PlayerGeneral> OnDeath;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServer)
        {
            gameState = GameObject.Find("GameState").GetComponent<GameState>();
        }
    }

    [Server]
    public void SvChangeHP(float _value, GameObject shooter)
    {
        //daca playerul e mort, nu are sens sa-i calculam damageul sau sa-i incrementam nr de morti
        if (!isAlive || (shooter != null && shooter.GetComponent<PlayerGeneral>().TeamID == TeamID))
            return;

        //_value e negativa asta inseamna ca trebuie sa cal
        if (_value < 0)
        {
            //_value e negativa, deci trebuie -_value pentru a obtine o valoare pozitiva
            if (-_value * armorPercentage / 100 >= ArmorSynced)
            {
                //scadem din damage toata armura deoarece armura nu poate sustine tot damageul
                HPsynced += _value + ArmorSynced;
                SvChangeArmor(-ArmorSynced);
            }
            else
            {
                //O parte din damage o sustine armura
                HPsynced += _value * (100 - armorPercentage) / 100;
                SvChangeArmor(_value * armorPercentage / 100);
            }

        }
        else
        {
            HPsynced += _value;
        }
        HPsynced = Mathf.Clamp(HPsynced, 0, maxHP);
        //moare playerul
        if (HPsynced == 0)
        {
            Die();
            ChangeDeaths(1);
            //daca nu s-a sinucis si l-a omorat un player se pune ca si kill
            if (shooter && shooter != gameObject)
            {
                PlayerGeneral killerI = shooter.GetComponent<PlayerGeneral>();
                killerI.ChangeFrags(1);

                OnDeath?.Invoke(this, killerI);
            }
            else
            {
                OnDeath?.Invoke(this, null);
            }

            //daca runda nu s-a terminat, peste RespawnTime secunde apeleaza functia, de asemenea, in cadrul functiei verifica daca runda s-a terminat sau nu
            //in caz ca runda s-a terminat in timpul respawn cycle-ului
            if (gameState.currentRoundStatus == GameState.ROUND_STATUS.ONGOING)
                Invoke(nameof(SetupRespawn), RespawnTime);
        }
    }

    //schimba armura de pe server, clientul nu o poate schimba
    [Server]
    public void SvChangeArmor(float _value)
    {
        ArmorSynced += _value;
        ArmorSynced = Mathf.Clamp(ArmorSynced, 0, maxArmor);
    }

    //este chemata pe client pentru a afisa noul hp
    void OnHPChange(float _Old, float _New)
    {
        ps.SetHP(HPsynced);
    }
    
    //idem
    void OnArmorChange(float _Old, float _New)
    {
        ps.SetArmor(ArmorSynced);
    }

    //daca se cheama functia de moarte pe server, clientul moare, adica i se da lock la camera si nu mai poate da niciun input
    //in caz de respawn, obiectul este teleportat la una dintre spawn positionurile din scena/ de pe harta
    void OnLifeChange(bool _Old, bool _New)
    {
        if (isAlive == true)
        {
            ps.Respawn();
        }
        else
        {
            ps.Death();
        }
    }

    //functie facuta in caz ca va fi nevoie in viitor
    void OnTeamChange(int _Old, int _New)
    {

    }
    public void ChangeFrags(int _value)
    {
        ps.Frags += _value;
    }
    public void ChangeDeaths(int _value)
    {
        ps.Deaths += _value;
    }
    public int GetFrags()
    {
        return ps.Frags;
    }
    public int GetDeaths()
    {
        return ps.Deaths;
    }


    //initial creata pentru a distruge "sticky bomburile" de la shotgun, de asemenea reseteaza HPul, armura cat si gloantele playerului
    [Server]
    void SetupRespawn()
    {
        if (gameState.currentRoundStatus == GameState.ROUND_STATUS.ONGOING)
        {
            ps.SetupRespawn();
            HPsynced = maxHP;
            ArmorSynced = 0;
            Respawn();
        }
    }

    public void Die()
    {
        isAlive = false;
    }

    public void Respawn()
    {
        isAlive = true;
    }

    //functie apelata in GameState pentru a da setup la obiect si de a-i activa UIul pentru runda activa (arata HP, Armura, Ammo)
    [Server]
    public void SvStartRound()
    {
        SetupRespawn();
        ps.TargetStartRoundUI();
    }
    //Omoara tot playerii vii si le activeaza UIul de endscreen
    [Server]
    public void SvEndRound(string winnerInfo)
    {
        if (isAlive)
            Die();
        ps.TargetEndRoundUI(winnerInfo);
    }
    //Playerul voteaza ca runda viitoare sa fie DM
    [Command]
    public void VoteDM()
    {
        gameState.VoteDeathmatch();
    }
    //Playerul voteaza ca runda viitoare sa fie TDM
    [Command]
    public void VoteTDM()
    {
        gameState.VoteTeamDeathmatch();
    }
}