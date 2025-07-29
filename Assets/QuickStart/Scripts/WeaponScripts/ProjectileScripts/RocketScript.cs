using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

public class RocketScript : ProjectileBase
{
    PlayerGeneral playerInterface;
    public float explosionRadius;
    public float explosionForce;
    public float maxForceMagnitude;
    Collider[] hitColliders;
    int projLI, lplayerLI, playerLI, wallLI;
    void Start()
    {
        projLI = 6; // projectile Layer int
        playerLI = 7; // other players Layer int
        lplayerLI = 8; // local player Layer int
        wallLI = 3; // mapGeometry Layer int
        Invoke(nameof(DestroyObject), lifeTime);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
            return;
        SvExplosion();
    }

    [Server]
    void SvExplosion()
    {
        hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            //check for trigger because we only want to account for physics object/players once
            if (hitCollider.gameObject.layer != wallLI || hitCollider.isTrigger)
            {
                Vector3 hitDirection = hitCollider.gameObject.transform.position - transform.position;
                if (explosionRadius - hitDirection.magnitude > 0)
                {
                    //must do this since server could be client too
                    if (hitCollider.gameObject.layer == playerLI || hitCollider.gameObject.layer == lplayerLI)
                    {
                        playerInterface = hitCollider.gameObject.GetComponent<PlayerGeneral>();

                        playerInterface.pm.Knockback(
                            hitDirection,
                            Mathf.Clamp(
                                Mathf.Lerp(0, explosionForce, (explosionRadius - hitDirection.magnitude) / explosionRadius),
                                0,
                                maxForceMagnitude
                            )
                        );

                        playerInterface.SvChangeHP(-(int)Mathf.Lerp(0, damage, (explosionRadius - hitDirection.magnitude) / explosionRadius), Shooter);
                    }
                    //rockets will be able to destroy sticky traps
                    else if (hitCollider.gameObject.layer == projLI)
                    {
                        if (hitCollider.gameObject.tag == "Destructible")
                        {
                            DestructibleProjectileBase dProjBase = hitCollider.gameObject.GetComponent<DestructibleProjectileBase>();
                            dProjBase.HandleDestruction();
                        }
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
        NetworkServer.Destroy(gameObject);
    }
    [Server]
    void DestroyObject()
    {
        if (!gameObject)
            return;
        NetworkServer.Destroy(gameObject);
    }

}
