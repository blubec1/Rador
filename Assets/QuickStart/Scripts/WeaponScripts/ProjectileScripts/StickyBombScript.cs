using QuickStart;
using UnityEngine;
using Mirror;

public class StickyBombScript : DestructibleProjectileBase
{

    public float explosionForce, explosionRadius, maxForceMagnitude;
    int wallLI = 3, playerLI = 7, lplayerLI = 8, projLI = 6;
    public override void HandleDestruction()
    {
        if (Shooter)
        {
            PlayerGeneral PI = Shooter.GetComponent<PlayerGeneral>();
            Shotgun sScript = PI.ps.GetWeapon(PlayerScript.WEAPONS.SHOTGUN).GetComponent<Shotgun>();
            sScript.stickyBombs.Remove(gameObject);
        }
        if(gameObject)
            NetworkServer.Destroy(gameObject);
    }

    public void Detonate()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            //verificam daca e trigger deoarece toti playerii au un trigger collider pentru a detecta cand atinge un Pick Up
            if (hitCollider.gameObject.layer != wallLI || hitCollider.isTrigger)
            {
                Vector3 hitDirection = hitCollider.gameObject.transform.position - transform.position;
                if (explosionRadius - hitDirection.magnitude > 0)
                {
                    //trebuie facut deoarece serverul poate fi si client
                    if (hitCollider.gameObject.layer == playerLI || hitCollider.gameObject.layer == lplayerLI)
                    {
                        PlayerGeneral PG = hitCollider.gameObject.GetComponent<PlayerGeneral>();
                        if (PG == null)
                            return;
                        PG.pm.Knockback(
                            hitDirection,
                            Mathf.Clamp(
                                Mathf.Lerp(0, explosionForce, (explosionRadius - hitDirection.magnitude) / explosionRadius),
                                0,
                                maxForceMagnitude
                            )
                        );

                        PG.SvChangeHP(-(int)Mathf.Lerp(0, damage, (explosionRadius - hitDirection.magnitude) / explosionRadius), Shooter);
                    }
                    else
                    {
                        Rigidbody rb;
                        if (!hitCollider.gameObject.GetComponent<Rigidbody>())
                            continue;
                        rb = hitCollider.gameObject.GetComponent<Rigidbody>();
                        rb.AddForce(
                            hitDirection.normalized *
                            Mathf.Clamp(Mathf.Lerp(0, explosionForce, (explosionRadius - hitDirection.magnitude) / explosionRadius), 0, maxForceMagnitude),
                            ForceMode.Impulse
                        );
                    }
                }
            }
        }

    }

}

