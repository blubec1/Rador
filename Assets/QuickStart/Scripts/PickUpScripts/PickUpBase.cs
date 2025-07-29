using Mirror;
using UnityEngine;

public abstract class PickUpBase : NetworkBehaviour
{
    protected int lplayerLI = 8, playerLI = 7;
    public float respawnTime;
    protected PlayerGeneral PI;
}
