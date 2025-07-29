using Mirror.Examples.Basic;
using Mirror.Examples.CCU;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public class PhysicsMovementAnimator : NetworkBehaviour
{
    [Header("References")]
    public Transform characterModel;
    public Transform playerCamera;
    public PlayerGeneral PG;

    [Header("Torso Aiming")]
    public float maxTorsoTwist = 60f;
    public float modelTurnSpeed = 8f;

    private Animator animator;
    public Transform head1;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;
        UpdateLegAnimation();
    }

    void LateUpdate()
    {
        AimHead();
    }

    void UpdateLegAnimation()
    {
        Vector3 velocity = PG.pm.CC.velocity;
        Vector3 horizontalVelocity = new(velocity.x, 0, velocity.z);

        if (horizontalVelocity.magnitude > 0.3f)
        {
            Vector3 localVel = characterModel.InverseTransformDirection(horizontalVelocity);
            animator.SetFloat("Forward", localVel.z);
            animator.SetFloat("Strafe", localVel.x);
        }
        else
        {
            animator.SetFloat("Forward", 0f);
            animator.SetFloat("Strafe", 0f);
        }

        animator.SetBool("Grounded", PG.pm.grounded);
        animator.SetBool("JumpReq", PG.pm.shouldJump);
        animator.SetBool("DoubleJump", PG.pm.should2Jump);
        animator.SetBool("isDead", !PG.isAlive);
    }

    void AimHead()
    {
        head1.rotation = playerCamera.rotation;
    }
}
