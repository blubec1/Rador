using Mirror;
using Mirror.BouncyCastle.Pqc.Crypto.Utilities;
using QuickStart;
using UnityEngine;

public class AmmoPickup : PickUpBase
{
    // 0 for Shotgun
    // 1 for Rocket Launcher
    public int weaponIDX;
    public int ammoValue;
    Weapon weaponScript;
    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (other.gameObject.layer == playerLI || other.gameObject.layer == lplayerLI)
        {
            PI = other.gameObject.GetComponent<PlayerGeneral>();
            if (PI.isAlive)
            {
                weaponScript = PI.ps.weaponArray[weaponIDX].GetComponent<Weapon>();
                weaponScript.weaponAmmo += ammoValue;
                weaponScript.weaponAmmo = Mathf.Clamp(weaponScript.weaponAmmo, 0, weaponScript.maxWeaponAmmo);
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