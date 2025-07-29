using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.Examples.Basic;
using Mirror.Examples.Tanks;
using UnityEngine;

namespace QuickStart
{
    public class Shotgun : Weapon
    {
        public int bulletCount, damagePerBullet, maxStickyBombs;
        public float spread, bulletForce;
        public GameObject stickyBombPrefab;
        GameObject hitFX;
        Vector3 rayVector;
        LayerMask LM;
        RaycastHit hit;
        int wallLI = 3, projLI = 6, lplayerLI = 7, playerLI = 8;
        public LinkedList<GameObject> stickyBombs = new();

        void Start()
        {
            LM = LayerMask.GetMask("ProxyPlayer", "LocalPlayer", "MapGeometry");
        }
        [Server]
        public override void Fire()
        {
            weaponAmmo -= 1;
            for (int i = 0; i < bulletCount; ++i)
            {
                // spread circular  prin gasirea random a aunui unghi, apoi rotind vectorul initial de rotatie in jurul acelei axe pentru un numar random de grade
                float angle = Random.Range(0, 360) * Mathf.Deg2Rad;

                float angleX = Mathf.Sin(angle);
                float angleY = Mathf.Cos(angle);

                float magnitude = Random.Range(-spread, spread);

                Vector3 worldSpaceRotatingAxis = angleX * weaponFirePosition.right + angleY * weaponFirePosition.up;

                rayVector = Quaternion.AngleAxis(magnitude, worldSpaceRotatingAxis) * weaponFirePosition.forward;

                Quaternion rotation = Quaternion.LookRotation(hit.normal);


                if (Physics.Raycast(weaponFirePosition.position, rayVector, out hit, 100f, LM))
                {
                    hitFX = Instantiate(hitEffect, hit.point, rotation);
                    NetworkServer.Spawn(hitFX);
                    var coroutine = DestroyObject(hitFX, FXLife);
                    StartCoroutine(coroutine);
                    //daca loveste un player, nu mai creeaza sticky bomburi
                    if (hit.collider.gameObject.layer == playerLI || hit.collider.gameObject.layer == lplayerLI)
                    {
                        PlayerGeneral PG = hit.collider.gameObject.GetComponent<PlayerGeneral>();
                        PG.SvChangeHP(-damagePerBullet, shooter);
                        PG.pm.Knockback(hit.collider.gameObject.transform.position - shooter.transform.position, bulletForce);
                        continue;
                    }
                    //daca nu loveste un player, creeaza un sticky bomb
                    GameObject stickyBomb = Instantiate(stickyBombPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    ProjectileBase pScript = stickyBomb.GetComponent<ProjectileBase>();
                    pScript.Shooter = shooter;
                    
                    stickyBombs.AddLast(stickyBomb);
                    if (maxStickyBombs < stickyBombs.Count)
                    {
                        //sterge si da pop in lista la element pentru a nu trece de limita de sticky bomburi
                        NetworkServer.Destroy(stickyBombs.First.Value);
                        stickyBombs.RemoveFirst();
                    }
                    NetworkServer.Spawn(stickyBomb);

                }
            }
        }

        //detoneaza sticky bomburile
        public override void AltFire()
        {
            foreach (GameObject bomb in stickyBombs)
            {
                StickyBombScript sbScript = bomb.GetComponent<StickyBombScript>();
                sbScript.Detonate();
                if (bomb)
                    NetworkServer.Destroy(bomb);
            }

            while (stickyBombs.Count > 0)
                stickyBombs.RemoveFirst();
        }

        public override void SetupRespawn()
        {
            ResetAmmo();
            foreach (GameObject bomb in stickyBombs)
            {
                if(bomb)
                    NetworkServer.Destroy(bomb);
            }

            while (stickyBombs.Count > 0)
                stickyBombs.RemoveFirst();
        }

        //deletes the stickyBombs left in the world
        public override void HandleDisconnect()
        {
            foreach (GameObject bomb in stickyBombs)
            {
                if (bomb)
                    NetworkServer.Destroy(bomb);
            }

            while (stickyBombs.Count > 0)
                stickyBombs.RemoveFirst();
        }

        IEnumerator DestroyObject(GameObject obj, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (obj)
                NetworkServer.Destroy(obj);
        }
    }
}
