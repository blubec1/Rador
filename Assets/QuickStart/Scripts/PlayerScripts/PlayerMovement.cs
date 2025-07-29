using Mirror;
using UnityEngine;
using UnityEngine.Timeline;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float maxSpeed;
    public float jumpCooldown;
    public float jumpForce;
    public float groundDrag;
    public float airDrag;
    public float maxSlopeAngle;
    public float antiBump;
    public CharacterController CC;
    public float acceleration, airAccelerate;
    public float friction;
    public float jumpBuffer = 0.1f;
    public PlayerGeneral PI;
    public bool shouldJump = false;
    public bool should2Jump = false;
    const float groundHeight = 1.6f;
    public float sphereRadius = 0.5f, sphereDistance = 0.65f, playerSphereRadius = 0.8f, playerSphereDistance = 0.5f;
    float horizontalInput;
    float verticalInput;
    float gAccel = -9.81f;
    int jumpcnt;
    public bool grounded, jumpRequest = false;
    bool collisionCheck;
    const int wallLI = 3, playerLI = 7, pickUpLI = 9;
    Vector3 MoveDirection;
    LayerMask groundLM, playerLM;
    RaycastHit groundInfo, slopeInfo;
    Vector3 finalVelo;
    ControllerColliderHit wallHit;

    enum GROUND_STATUS
    {
        AIR,
        FLAT,
        SLOPE,
        STEEP
    }
    GROUND_STATUS prevslope, slopeStatus;
    void Start()
    {
        groundLM = LayerMask.GetMask("MapGeometry");
        playerLM = LayerMask.GetMask("ProxyPlayer");
        jumpcnt = 2;
    }
    void Update()
    {
        if (!isLocalPlayer)
            return;
        if (PI.isAlive)
            GetInput();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;
        MovePlayer();
    }

    //aplica o "forta" playerului
    [TargetRpc]
    public void Knockback(Vector3 direction, float force)
    {
        if (isLocalPlayer)
            finalVelo += direction.normalized * force;
    }
    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded)
            {
                shouldJump = true;
                jumpcnt = 1;
            }
            else if (jumpcnt == 1)
            {
                should2Jump = true;
                jumpcnt = 0;
            }
        }
    }
    //Functia principala de miscare a playerului
    private void MovePlayer()
    {
        MoveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        MoveDirection = MoveDirection.normalized;
        MoveDirection *= moveSpeed;
        slopeStatus = SlopeCheck();
        grounded = GroundCheck();
        Debug.DrawRay(transform.position, finalVelo, Color.green);
        Debug.DrawRay(transform.position, MoveDirection, Color.red);
        if (grounded)
        {
            CalcMoveGround();
            if (slopeStatus != GROUND_STATUS.STEEP)
                Friction();
        }
        else
        {
            Gravity();
            CalcMoveAir();
        }

        if (shouldJump)
        {
            Jump();
            shouldJump = false;
        }
        else if (should2Jump)
        {
            DoubleJump();
            should2Jump = false;
        }


        if (collisionCheck)
        {
            collisionCheck = false;
            CalcWallCollision();
        }

        CC.Move(finalVelo * Time.deltaTime);
    }
    private void Gravity()
    {
        finalVelo.y += gAccel * Time.deltaTime;
    }

    //  GroundCheck e facut astfel incat obtine informatii fata de pamant si foloseste o sfera sub obiectul playerului pentru a detecta daca este pe pamant
    // OR-ul este pentru a rezolva problema cand se afla deasupra altui player
    private bool GroundCheck()
    {
        Physics.Raycast(transform.position, Vector3.down, out groundInfo, groundLM);
        return Physics.CheckSphere(gameObject.transform.position - new Vector3(0, sphereDistance, 0), sphereRadius, groundLM) || Physics.CheckSphere(gameObject.transform.position - new Vector3(0, playerSphereDistance, 0), playerSphereRadius, playerLM);
    }

    // miscarea pe pamant
    // foloseste Dot Product pentru a face schimbarea de directie mai smooth si accelerarea sa nu fie instantanee 
    // (sinusul unghiului modifica rezultatul, oferind o schimbare de directie mai brusca ori mai lenta).
    //  De asemenea, controleaza si miscarea pe rampe
    private void CalcMoveGround()
    {
        switch (slopeStatus)
        {
            case GROUND_STATUS.FLAT: break;
            case GROUND_STATUS.SLOPE:
                Vector3 slopeDirection = Vector3.ProjectOnPlane(MoveDirection, groundInfo.normal).normalized;
                if (slopeDirection.y < 0)
                {
                    MoveDirection = slopeDirection * MoveDirection.magnitude;
                    //cand playerul se duce in jos pe o rampa, inclinam inputul inspre rampa pentru a-l tine lipit de rampa
                    MoveDirection.y -= 2f;
                }
                break;
            case GROUND_STATUS.STEEP:
                CalcMoveAir();
                Gravity();
                finalVelo = Vector3.ProjectOnPlane(finalVelo, groundInfo.normal);
                return;

        }
        float dotspeed = Vector3.Dot(finalVelo, MoveDirection.normalized);
        float speed = MoveDirection.magnitude - dotspeed;
        if (speed <= 0)
            return;
        speed *= acceleration;
        finalVelo += MoveDirection * speed;
    }
    // acelasi lucru ca la miscarea pe pamant, doar ca nu e forta de frecare
    // astfel conserva viteza motivand bunny hopping (sa sari odata ce ai atins pamantul)
    // de asemenea este adaugat si air strafing prin apasarea tastei A sau D si miscarea lenta a mouseului in directia respectiva
    // astfel se poate obtine o viteza extraordinar de mare si se poate controla mult mai usor traiectoria caracterului in aer
    // se limiteaza wishSpeed pentru a nu da un bonus de viteza prea mare pe fiecare frame
    private void CalcMoveAir()
    {
        float wishSpeed = MoveDirection.magnitude;
        if (wishSpeed > 3f)
            wishSpeed = 3f;
        float dotSpeed = Vector3.Dot(finalVelo, MoveDirection);
        float addSpeed = wishSpeed - dotSpeed;
        if (addSpeed <= 0)
            return;
        finalVelo += MoveDirection.normalized * wishSpeed;

    }
    private void Jump()
    {
        finalVelo.y = Mathf.Sqrt(jumpForce * -gAccel);
    }
    private void DoubleJump()
    {
        finalVelo.x = MoveDirection.x;
        finalVelo.z = MoveDirection.z;
        finalVelo.y = Mathf.Sqrt(jumpForce * -gAccel);
    }
    private void Friction()
    {
        finalVelo = Vector3.Lerp(finalVelo, Vector3.zero, friction);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        wallHit = hit;
        collisionCheck = true;
    }

    //partea de cod care omoara momentumul cand playerul intra intr-un peret in aer, fie el vertical sau deasupra capului playerului
    void CalcWallCollision()
    {
        if (wallHit.collider != null)
            if (!grounded && (wallHit.collider.gameObject.layer == wallLI || wallHit.collider.gameObject.layer == playerLI))
            {
                //cand playerul intalneste un perete inclinat inspre el si are o viteza pe y pozitiva, n-ar trebui sa-i fie omorat momentumul ascendent 
                if (finalVelo.y >= 1f)
                {
                    Vector3 veloxyz = new(finalVelo.x, finalVelo.y, finalVelo.z);
                    veloxyz = Vector3.ProjectOnPlane(veloxyz, wallHit.normal);
                    finalVelo.x = veloxyz.x;
                    finalVelo.z = veloxyz.z;
                    if (2f < veloxyz.y)
                        finalVelo.y = veloxyz.y;
                    else if (finalVelo.y > 0)
                        finalVelo.y = 0;
                }
                //altfel modfiica doar miscarea pe x si z cat sa nu dea slide pa langa perete
                else
                {
                    Vector3 veloxz = new(finalVelo.x, 0, finalVelo.z);
                    veloxz = Vector3.ProjectOnPlane(veloxz, wallHit.normal);
                    finalVelo.x = veloxz.x;
                    finalVelo.z = veloxz.z;
                }
            }
    }
    //verifica tipul slopeului pe care se afla playerul
    GROUND_STATUS SlopeCheck()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeInfo, 3f, groundLM))
        {
            if (slopeInfo.collider.gameObject.layer == playerLI)
            {
                return GROUND_STATUS.FLAT;
            }

            float angle = Vector3.Angle(slopeInfo.normal, Vector3.up);
            if (angle == 0)
                return GROUND_STATUS.FLAT;
            else if (0 < angle && angle <= maxSlopeAngle)
                return GROUND_STATUS.SLOPE;
            return GROUND_STATUS.STEEP;
        }
        else
            return GROUND_STATUS.AIR;
    }

    public Vector3 GetWorldVelo()
    {
        return finalVelo;
    }

    public Vector3 GetLocalVelo()
    {
        return transform.InverseTransformDirection(finalVelo);
    }
}
