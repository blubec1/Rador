using Mirror;
using UnityEngine;

public class ProjectileBase : NetworkBehaviour
{
    public float speed;
    [SyncVar]
    public GameObject Shooter;
    public float damage;
    public float lifeTime;
}