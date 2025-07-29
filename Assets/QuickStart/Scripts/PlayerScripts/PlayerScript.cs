using Mirror;
using Mirror.Examples.Basic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace QuickStart
{
    public class PlayerScript : NetworkBehaviour
    {
        public TextMeshPro playerNameText;
        public GameObject floatingInfo;
        public Transform Player_Eyes;

        private Material playerMaterialClone;
        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;
        [SyncVar(hook = nameof(OnFragsChanged))]
        public int Frags;
        [SyncVar(hook = nameof(OnDeathsChanged))]
        public int Deaths;
        public float HP, Armor;
        private int selectedWeaponLocal = 1;
        public GameObject[] weaponArray;
        public GameObject[] weaponModels;
        public GameObject[] weaponViewModels;
        public GameObject[] objectsToHide;

        [SyncVar(hook = nameof(OnWeaponChanged))]
        public int activeWeaponSynced = 1;
        private SceneScript sceneScript;
        private Weapon activeWeapon;
        private float mouseX, mouseY, xRot, yRot;
        public GameObject playerModel, backpack, mask, rl, vmrl, sht, vmsht;
        public float sensX, sensY;
        public PlayerGeneral PG;
        LayerMask wallLM, cameraLM;

        public enum WEAPONS
        {
            NONE,
            SHOTGUN,
            ROCKETLAUNCHER
        }

        //activat pentru fiecare client in parte pentru a da enable la arma activa si a da disable la arma veche indiferent daca este sau nu LocalPlayer
        void OnWeaponChanged(int _Old, int _New)
        {
            // disable old weapon
            // in range and not null
            if (0 <= _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)
            {
                weaponArray[_Old].SetActive(false);
                weaponModels[_Old].SetActive(false);
                weaponViewModels[_Old].SetActive(false);
            }
                // enable new weapon
                // in range and not null
                if (0 <= _New && _New < weaponArray.Length && weaponArray[_New] != null)
                {
                    weaponArray[_New].SetActive(true);
                    //daca este localplayer, nu ar trebui sa vada modelul real al armei
                    if (!isLocalPlayer)
                    weaponModels[_New].SetActive(true);
                    //daca nu este localplayer, nu ar trebui sa vada arma care se misca dupa capul Localplayerului
                    if (isLocalPlayer)
                    weaponViewModels[_New].SetActive(true);
                    activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                    if (isLocalPlayer)
                        sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                }
        }
        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            playerMaterialClone = new Material(GetComponent<Renderer>().material);
            playerMaterialClone.color = _New;
            GetComponent<Renderer>().material = playerMaterialClone;
        }
        public override void OnStartLocalPlayer()
        {
            //pentru ca localplayer sa nu se vada pe sine
            cameraLM = LayerMask.GetMask("ProxyPlayer", "MapGeometry", "Projectile", "UI", "Default", "PickUps", "ViewModel");
            Camera.main.cullingMask = cameraLM;
            gameObject.layer = 8;
            playerModel.layer = 8;
            backpack.layer = 8;
            mask.layer = 8;
            rl.layer = 8;
            sht.layer = 8;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.position = Player_Eyes.position;

            floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            string name = "Player" + Random.Range(100, 999);
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            CmdSetupPlayer(name, color);
            Debug.Log(PG.gameObject);
            sceneScript.PG = PG;
        }
        //empty until I will need to use them
        public void OnFragsChanged(int _Old, int _New)
        {

        }
        public void OnDeathsChanged(int _Old, int _New)
        {

        }
        public void SetHP(float value)
        {
            if (!isLocalPlayer)
                return;
            HP = value;
            sceneScript.UIHP((int)HP);
        }

        public void SetArmor(float value)
        {
            if (!isLocalPlayer)
                return;
            Armor = value;
            sceneScript.UIArmor((int)Armor);
        }

        [Command]
        public void CmdChangeActiveWeapon(int newIndex)
        {
            activeWeaponSynced = newIndex;
        }
        void Awake()
        {
            sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;
            Debug.Log(sceneScript.gameObject);
            Debug.Log(PG.gameObject);
            foreach (var item in weaponArray)
                if (item != null)
                    item.SetActive(false);
            if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
            {
                activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                sceneScript.UIHP((int)HP);
                sceneScript.UIArmor((int)Armor);
            }
        }

        [Command]
        public void CmdSendPlayerMessage()
        {
            if (sceneScript)
                sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
        }

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            // player info sent to server, then server updates sync vars which handles it on all clients
            playerName = _name;
            playerColor = _col;
            sceneScript.statusText = $"{playerName} joined.";
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Start()
        {
            LockCursor();
            //daca nu esti localplayer nu ar trebui sa vezi viewmodelele care ar trebui vazute numai de localplayer
            if (isLocalPlayer)
                return;
            vmrl.SetActive(false);
            vmsht.SetActive(false);
        }

        void Update()
        {   
            //daca nu esti local player, inputul tau n-ar trebui sa afecteze alte obiecte controlate de alti playeri
            if (!isLocalPlayer)
            {
                floatingInfo.transform.LookAt(Camera.main.transform);
                return;
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
                mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;
            }
            xRot -= mouseY;
            yRot += mouseX;
            xRot = Mathf.Clamp(xRot, -90f, 90f);


            //daca e mort, fixeaza-i camera, dar nu cat sa dea clip prin pereti
            if (!PG.isAlive)
            {
                RaycastHit ceilingInfo;
                float additionalDist;
                if (Physics.Raycast(transform.position, Vector3.up, out ceilingInfo, 3f, wallLM))
                    additionalDist = ceilingInfo.distance;
                else
                    additionalDist = 3f;
                Camera.main.transform.position = new(
                    transform.position.x,
                    transform.position.y + additionalDist,
                    transform.position.z
                );
                Camera.main.transform.LookAt(gameObject.transform);
            }

            //daca e viu, activeaza-i controlul asupra obiectului
            if (PG.isAlive)
            {
                Player_Eyes.rotation = Quaternion.Euler(xRot, yRot, 0);
                transform.rotation = Quaternion.Euler(0, yRot, 0);
                Camera.main.transform.position = Player_Eyes.position;
                Camera.main.transform.rotation = Player_Eyes.rotation;
                GunInput();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                sceneScript.ShowScoreBoard();
            }
            else if(Input.GetKeyUp(KeyCode.Tab))
            {
                sceneScript.HideScoreBoard();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.None)
                {
                    LockCursor();
                }
                else
                {
                    UnlockCursor();
                }
            }
        }

        void GunInput()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    selectedWeaponLocal = 0;
                    CmdChangeActiveWeapon(selectedWeaponLocal);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    selectedWeaponLocal = 1;
                    CmdChangeActiveWeapon(selectedWeaponLocal);
                }


                if (Input.GetKeyDown(KeyCode.Mouse0)) //Fire1 este click stanga
                {
                    if (activeWeapon && Time.time > activeWeapon.FireCooldown && activeWeapon.weaponAmmo > 0)
                    {
                        activeWeapon.FireCooldown = Time.time + activeWeapon.FireCooldownTime;
                        CmdShootRay(1);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Mouse1)) //Fire2 este click dreapta
                {
                    if (activeWeapon && Time.time > activeWeapon.FireCooldown && activeWeapon.weaponAmmo > 0)
                    {
                        activeWeapon.FireCooldown = Time.time + activeWeapon.altFireCooldownTime;
                        CmdShootRay(2);
                    }
                }
            }

        }

        //fire the gun on the server so it automatically syncs on all other clients
        [Command]
        void CmdShootRay(int mode)
        {
            switch (mode)
            {
                case 1:
                    activeWeapon.Fire();
                    break;

                case 2:
                    activeWeapon.AltFire();
                    break;
            }
        }

        public void UpdateUIAmmo()
        {
            if (isLocalPlayer)
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }

        public void Death()
        {

            if (isLocalPlayer)
            {
                Camera.main.transform.position = new(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + 2f,
                Camera.main.transform.position.z
            );
            }

        }

        //cand moare, dezactiveaza obiectul ca sa nu apara probleme la teleportare
        public void Respawn()
        {
            foreach (var obj in objectsToHide)
            {
                obj.SetActive(false);
            }

            if (isLocalPlayer)
            {
                transform.position = NetworkManager.startPositions[Random.Range(0, NetworkManager.startPositions.Count)].position;
            }

            foreach (var obj in objectsToHide)
            {
                obj.SetActive(true);
            }

        }

        //folosit pentru a reseta lucruri ca gloante sau "sticky bomburi"
        public void SetupRespawn()
        {
            Weapon wScript = weaponArray[0].GetComponent<Weapon>();
            wScript.weaponAmmo = 10;
            for (int i = 1; i < weaponArray.Length; ++i)
            {
                wScript = weaponArray[i].GetComponent<Weapon>();
                wScript.SetupRespawn();
            }
        }

        public void HandleDisconnect()
        {
            Weapon wScript;
            for (int i = 0; i < weaponArray.Length; ++i)
            {
                wScript = weaponArray[i].GetComponent<Weapon>();
                wScript.HandleDisconnect();
            }
        }
        public GameObject GetWeapon(WEAPONS weapon)
        {
            switch (weapon)
            {
                case WEAPONS.SHOTGUN:
                    return weaponArray[0];
                case WEAPONS.ROCKETLAUNCHER:
                    return weaponArray[1];
            }
            return null;
        }
        [TargetRpc]
        public void TargetStartRoundUI()
        {
            sceneScript.StartRoundUI();
        }

        [TargetRpc]
        public void TargetEndRoundUI(string winnerInfo)
        {
            sceneScript.EndRoundUI(winnerInfo);
        }

    }
}