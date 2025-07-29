using System;
using System.Collections;
using Mirror;
using UnityEngine;


public class HealthPickup : PickUpBase
{
    public int HPValue;
    private IEnumerator coroutine;


    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (other.gameObject.layer == lplayerLI || other.gameObject.layer == playerLI)
        {
            PI = other.gameObject.GetComponent<PlayerGeneral>();
            if (PI.isAlive)
            {
                PI.SvChangeHP(HPValue, null);
                DisableObject();
                Invoke(nameof(EnableObject), respawnTime);
            }
        }
    }

    [ClientRpc]
    void EnableObject()
    {
        gameObject.SetActive(true);
    }
    [ClientRpc]
    void DisableObject()
    {
        gameObject.SetActive(false);
    }

}