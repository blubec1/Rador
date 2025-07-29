using Mirror;
using UnityEngine;

namespace QuickStart
{
    public abstract class Weapon : NetworkBehaviour
    {
        public float weaponSpeed;
        public float FXLife;
        public float FireCooldown;
        public float altFireCooldown;
        public float FireCooldownTime;
        public float altFireCooldownTime;
        [SyncVar(hook = nameof(OnAmmoChange))]
        public int weaponAmmo;
        public int maxWeaponAmmo;
        public GameObject shooter;
        public GameObject hitEffect;

        public abstract void Fire();

        public abstract void AltFire();

        public abstract void SetupRespawn();
        public abstract void HandleDisconnect();
        public Transform weaponFirePosition;

        public void OnAmmoChange(int _Old, int _New)
        {
            if (isLocalPlayer)
            {
                PlayerGeneral PI = shooter.GetComponent<PlayerGeneral>();
                PI.ps.UpdateUIAmmo();
            }
        }

        public void ChangeAmmo(int _value)
        {
            weaponAmmo += _value;
        }

        public void ResetAmmo()
        {
            weaponAmmo = 0;
        }

        public int GetAmmo()
        {
            return weaponAmmo;
        }
    }
}