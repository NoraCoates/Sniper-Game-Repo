using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//Note: Only the player who set up the LAN connnnection seems to be able to end it

public class PlayerController : NetworkBehaviour {
    
    public GameObject Bullet;
    public Transform BulletSpawn;

    public float ForwardSpeed = 8.0f;   // Speed when walking forward
    public float BackwardSpeed = 4.0f;  // Speed when walking backwards
    public float StrafeSpeed = 4.0f;    // Speed when walking sideways
    public float RunMultiplier = 2.0f;   // Speed when sprinting
    public float CrouchMultiplier = .5f;  // Speed when crouching
    public float jumpSpeed = 0.5f;
    public KeyCode RunKey = KeyCode.LeftShift;
    public KeyCode CrouchKey = KeyCode.C;
    public KeyCode JumpKey = KeyCode.Space;

    public Camera cam;
    [HideInInspector] public float CurrentTargetSpeed = 8f;
    private Rigidbody m_RigidBody;
    private CapsuleCollider m_Capsule;
    public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
    public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
    public float stickToGroundHelperDistance = 0.5f; // stops the character
    public float shellOffset; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
    public bool airControl;
    private bool m_Running, m_Crouching, m_PreviouslyGrounded, m_IsGrounded, m_MotionAllowed;
    private Vector3 m_GroundContactNormal;

    public float animSpeed = 1.5f;
    public Animator anim;
    private AnimatorStateInfo currentBaseState;
    public bool useCurves = true;
    public float useCurvesHeight = 0.5f;

    static int idleState = Animator.StringToHash("Base Layer.Idle");
    static int locoState = Animator.StringToHash("Base Layer.Locomotion");
    //static int restState = Animator.StringToHash("Base Layer.Rest");

    private float orgColHeight;
    private Vector3 orgVectColCenter;




	// Use this for initialization
	void Start () {
        m_RigidBody = GetComponent<Rigidbody>();
        m_Capsule = GetComponent<CapsuleCollider>();
        orgColHeight = m_Capsule.height;
        orgVectColCenter = m_Capsule.center;
        m_RigidBody.constraints = RigidbodyConstraints.FreezeRotationY;
        m_RigidBody.useGravity = true;

	}
    private Vector2 GetInput()
    {
        Vector2 input = new Vector2
        {
            x = Input.GetAxis("Horizontal"),
            y = Input.GetAxis("Vertical")
        };
        UpdateDesiredTargetSpeed(input);
        return input;
    }
    public void UpdateDesiredTargetSpeed(Vector2 input)
    {
        if (m_IsGrounded)
        {
            if (input == Vector2.zero)
            {
                return;
            }
            if (input.x > 0 || input.x < 0)
            {
                //strafe
                CurrentTargetSpeed = StrafeSpeed;
            }
            if (input.y < 0)
            {
                //backwards
                CurrentTargetSpeed = BackwardSpeed;
            }
            if (input.y > 0)
            {
                //forwards
                //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                CurrentTargetSpeed = ForwardSpeed;
            }
        }

    }
   
    void FixedUpdate()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        //are you on the ground?
        GroundCheck();

        //gen. movement

        var x = Input.GetAxis("Horizontal") * Time.deltaTime * StrafeSpeed;
        var z = 0f;
        if(Input.GetAxis("Vertical") > 0) {
            z = Input.GetAxis("Vertical") * Time.deltaTime * ForwardSpeed;
        } else {
            z = Input.GetAxis("Vertical") * Time.deltaTime * BackwardSpeed;
        }

        anim.SetFloat("Speed", Input.GetAxis("Vertical"));
        anim.SetFloat("Direction", Input.GetAxis("Horizontal"));
        anim.speed = animSpeed;
        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
        m_RigidBody.useGravity = true;

        //sprint opt
        if (Input.GetKey(RunKey))
        {
            z *= RunMultiplier;
            m_Running = true;
            cam.fieldOfView = 80f;
        }
        else
        {
            m_Running = false;
            cam.fieldOfView = 60f;
        }

        //jump
        //need to avoid double jump
        //need to decrease/disable ability to move while jumping
        if (Input.GetKey(JumpKey))
        {
            m_RigidBody.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
            float g = 0.0f;
            g = Mathf.Sin(Time.deltaTime);
            m_RigidBody.AddForce(Vector3.down * g * 10);
            GroundCheck();
        }


        //crouch
        if (Input.GetKey(CrouchKey))
        {
            m_Crouching = true;
            m_Capsule.height = .75f;
            cam.transform.localPosition = new Vector3(0, .6f, 0);
            z *= CrouchMultiplier;
        }
        else {
            m_Crouching = false;
            m_Capsule.height = 1.5f;
            cam.transform.localPosition = new Vector3(0, 1.2f, 0);
        }

        transform.Translate(x, 0, z);

        if (currentBaseState.fullPathHash == locoState)
        {
            if (useCurves)
            {
                ResetCollider();
            }
        }
        else if (currentBaseState.fullPathHash == idleState)
        {
            if (useCurves)
            {
                ResetCollider();
            }
        }

        //left click
        if (Input.GetMouseButtonDown(0))
        {
            CmdFire();
        }

    }



    private float SlopeMultiplier()
    {
        float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
        return SlopeCurveModifier.Evaluate(angle);
    }

    // This [Command] code is called on the Client but runs on the Server!
    [Command]
    void CmdFire()
    {
        var bullet = (GameObject)Instantiate(
            Bullet,
            BulletSpawn.position,
            BulletSpawn.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 9;
        // Spawn the bullet on the Clients
        NetworkServer.Spawn(bullet);
        // Destroy the bullet after 2 seconds
        Destroy(bullet, 1.5f);
    }
    private void GroundCheck()
    {
        m_PreviouslyGrounded = m_IsGrounded;
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - shellOffset), Vector3.down, out hitInfo,
                               ((m_Capsule.height / 2f) - m_Capsule.radius) + groundCheckDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            m_IsGrounded = true;
            m_GroundContactNormal = hitInfo.normal;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundContactNormal = Vector3.up;
        }
    }

    void ResetCollider()
    {
        m_Capsule.height = orgColHeight;
        m_Capsule.center = orgVectColCenter;
    }
}