using UnityEngine;
using Mirror;
using System.Collections;


public class ArmorPickup : PickUpBase
{
    public int armorValue;
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
                PI.SvChangeArmor(armorValue);
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