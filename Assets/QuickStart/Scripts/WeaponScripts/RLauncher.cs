using Mirror;
using UnityEngine;

namespace QuickStart
{
    public class RLauncher : Weapon
    {
        public float altFireKnockbackRadius, altFireKnockbackForce;
        Collider[] hitColliders;
        public GameObject rocket, rocketI;
        int lplayerLI = 8, playerLI = 7, wallLI = 3, projLI = 6;
        Vector3 altExplosionPosition;
        ProjectileBase proj;

        void Start()
        {
            wallLI = LayerMask.NameToLayer("MapGeometry");
            projLI = LayerMask.NameToLayer("Projectile");
            playerLI = LayerMask.NameToLayer("ProxyPlayer");
            lplayerLI = LayerMask.NameToLayer("LocalPlayer");
            proj = rocket.GetComponent<ProjectileBase>();
        }
        [Server]
        public override void Fire()
        {
            weaponAmmo -= 1;
            GameObject rocketI = Instantiate(rocket, weaponFirePosition.position, weaponFirePosition.rotation);
            ProjectileBase rScript = rocketI.GetComponent<ProjectileBase>();
            rocketI.GetComponent<Rigidbody>().linearVelocity = rocketI.transform.forward * rScript.speed;
            rScript.Shooter = shooter;
            Physics.IgnoreCollision(rocketI.GetComponent<Collider>(), shooter.GetComponent<Collider>());
            NetworkServer.Spawn(rocketI);
        }

        [Server]
        public override void AltFire()
        {
            hitColliders = Physics.OverlapSphere(
                shooter.transform.position + weaponFirePosition.forward,
                altFireKnockbackRadius
                );
            altExplosionPosition = shooter.transform.position + weaponFirePosition.transform.forward;

            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.gameObject.layer != wallLI && hitCollider.gameObject != shooter)
                {
                    //trebuie facut deoarece serverul poate fi si client
                    if (hitCollider.gameObject.layer == playerLI || hitCollider.gameObject.layer == lplayerLI)
                    {
                        Vector3 hitdirection = hitCollider.gameObject.transform.position - altExplosionPosition;
                        hitCollider.gameObject.GetComponent<PlayerMovement>().Knockback(
                            hitdirection,
                            (altFireKnockbackRadius - hitdirection.magnitude) * altFireKnockbackForce
                            );
                    }
                    else if (hitCollider.gameObject.layer == projLI)
                    {
                        Vector3 hitdirection = weaponFirePosition.forward;
                        RocketScript rs = hitCollider.gameObject.GetComponent<RocketScript>();
                        Physics.IgnoreCollision(hitCollider, rs.Shooter.GetComponent<Collider>(), false);
                        if (!hitCollider.gameObject.GetComponent<Rigidbody>())
                            return;
                        hitCollider.gameObject.GetComponent<Rigidbody>().linearVelocity = hitdirection * weaponSpeed;
                    }
                    else
                    {
                        Vector3 hitdirection = weaponFirePosition.forward;
                        if (!hitCollider.gameObject.GetComponent<Rigidbody>())
                            return;
                        hitCollider.gameObject.GetComponent<Rigidbody>().AddForce(
                            hitdirection.normalized * (altFireKnockbackRadius - hitdirection.magnitude) * altFireKnockbackForce,
                            ForceMode.Impulse
                            );
                    }
                }
            }
        }

        public override void SetupRespawn()
        {
            ResetAmmo();
        }
        //currently it does nothing
        public override void HandleDisconnect()
        {
            return;
        }
    }
}