using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    // ----------------------------------------------------------------------------------------------------------------------------
    // VARIABLES
    // ----------------------------------------------------------------------------------------------------------------------------
    [Header("Keyboard Input")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode slideKey = KeyCode.LeftControl;
    float horizontalInput;
    float verticalInput;
    [Header("Movement")]
    private float moveSpeed;
    public enum MovementState { walking, sprinting, crouching, sliding, air, wallrunning }
    public MovementState state;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float wallrunSpeed;
    public float groundDrag;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public bool readyToJump;
    float jumpmode = 0;
    private Vector3 jumpDirection;
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;
    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    public float slideDrag;
    private float slideTimer;
    private bool sliding;
    private Vector3 slideDirection;
    int slidesoundCounter = 0;
    [Header("Wallrunning")]
    public float wallRunForce;
    public float maxWallRunTime;
    private float wallRunTimer;
    public bool wallrunning;
    [Header("Wall Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallhit;
    private RaycastHit rightWallhit;
    private bool wallLeft;
    private bool wallRight;
    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    bool exitingSlope;
    [Header("Speedometer")]
    public Text SpeedText;
    [Header("Referencess")]
    public float playerHeight;
    public Transform orientation;
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;
    bool grounded;
    Vector3 moveDirection;
    Rigidbody rb;
    AudioManager playerAudio;

    // ----------------------------------------------------------------------------------------------------------------------------
    // START / UPDATE / FIXED UPDATE
    // ----------------------------------------------------------------------------------------------------------------------------

    void Start()
    {
        //on start, we will find the player's Rigidbody component
        rb = GetComponent<Rigidbody>();
        //we freeze the rotations so that it doesn't fall over. we are controlling every move the player makes
        rb.freezeRotation = true;

        //also grab the script that controlls player audio
        playerAudio = GetComponent<AudioManager>();

        //save the starting player height for returning from a crouch
        startYScale = transform.localScale.y;

        jumpDirection = transform.up;
    }

    private void Update()
    {        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        //always Run these Methods
        MyInput();
        SpeedControl();
        StateHandler(); //statehandler does audio triggers too
        CheckForWall();

        //handle drag & reset jump when grounded
        if (grounded && !sliding)
        {
            rb.drag = groundDrag;
            ResetJump();
        }
        else if (grounded && sliding)
        {
            rb.drag = slideDrag;
            ResetJump();
        }
        else
        {

            rb.drag = 0;
        }

        //wallrunning check
        if ((wallLeft || wallRight) && verticalInput > 0 && !grounded)
        {
            if (!wallrunning)
            {
                StartWallRun();
            }

        }
        else
        {
            if (wallrunning)
            {
                StopWallRun();
            }
        }

       
    }

    private void FixedUpdate()
    {
        MovePlayer();

        if (sliding)
        {
            verticalInput = 0;
            horizontalInput = 0;
            SlidingMovement();
        }

        if (wallrunning)
        {
            WallRunningMovement();
        }
        Debug.Log(rb.velocity.magnitude*2);
    }

    // ----------------------------------------------------------------------------------------------------------------------------
    // OTHER METHODS & THINGS
    // ----------------------------------------------------------------------------------------------------------------------------

    private void MyInput()
    {
        //WALK
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        //JUMP
        if(Input.GetKeyDown(jumpKey) && readyToJump)
        {
            readyToJump = false;
            jumpmode += 1;
            Jump();

            if (grounded || wallrunning)
            {
                playerAudio.PlayJump();
            }
            if (!grounded && !wallrunning)
            {
                playerAudio.PlayDoubleJump();
            }
            //Jump will repeat if key is held, but at a jumpCooldown interval.
            //Invoke(nameof(ResetJump), jumpCooldown);
        }
        //CROUCH START
        if (Input.GetKey(crouchKey) && moveSpeed < 7)
        {
            //when the player crouches, they shrink on the y
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            //the player shrinks to center, so apply force to get low quickly
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        //CROUCH STOP
        if (Input.GetKeyUp(crouchKey))
        {
            //return the player to normal size when key is released
            //"normal size" is collected on Start()
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
        //SLIDE START
        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && moveSpeed >= 7)
        {
            Vector3 slideDirection = moveDirection;
            StartSlide();
        }
        //SLIDE STOP
        if (Input.GetKeyUp(slideKey) && sliding)
        {
            StopSlide();
        }
    }
    private void StateHandler()
    {
        //Mode-Sliding
        if (sliding)
        {
            state = MovementState.sliding;
            if (OnSlope() && rb.velocity.y < 0.1f)
            {
                desiredMoveSpeed = slideSpeed;
            }
            else desiredMoveSpeed = sprintSpeed;
         }

        //Mode-Crouching
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }
        //Mode-Sprinting
        else if(grounded && Input.GetKey(sprintKey))
        {
            if (grounded && rb.velocity.magnitude * 2 > 19f && playerAudio.playersound.isPlaying == false)
            {
                playerAudio.PlaySprint();
            }

            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        //Mode-Walking
        else if (grounded)
        {
            if (grounded && rb.velocity.magnitude * 2 > 1f && grounded && rb.velocity.magnitude * 2 <18.9f && playerAudio.playersound.isPlaying == false)
            {
                playerAudio.PlayWalk();
            }

            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }
        //Mode-Wallrunning
        else if (wallrunning)
        {
            if (wallrunning && playerAudio.playersound.isPlaying == false)
            {
                playerAudio.PlaySprint();
            }

            state = MovementState.wallrunning;
            desiredMoveSpeed = wallrunSpeed;
            ResetJump();
        }
        //Mode-Air
        else
        {
            state = MovementState.air;
        }
        
        //check if the desiredMoveSpeed has changed drastically
        if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed !=0)
        { 
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }


    // --------MOVEMENT-----------------------
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        //smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time<difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime;
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }
    private void MovePlayer()
    {
        //movement direction is FORWARD/BACKWARD movement + LEFT/RIGHT movement
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if(OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * desiredMoveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f,ForceMode.Force);
            }
        }

        //move our rigidbody in the move direction * moveSpeed
        // vector3.normalized will return the same direction, but with a length of 1.0

        //on the ground apply a normal force
        if (grounded && !sliding)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        //in the air apply a modified force with airMultiplier
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        //turn gravity off while on a slope
        rb.useGravity = !OnSlope();
    }
    private void SpeedControl()
    {
        //CONTROL SPEED ON SLOPE
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }

        //CONTROL SPEED ON GROUND OR IN AIR
        else
        {
            //flatVel is only the X and Z movement speed
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //SPEEDOMETER CODE HERE
            float kilometerConversion = rb.velocity.magnitude * 2;
            //display speed on HUD
            SpeedText.text = "Speed:" + kilometerConversion.ToString("F0") + "kph";


            //if it is greater than our moveSpeed, then recalculate what it should be and apply
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }
    // --------SLOPES-----------------------
    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }
    private Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
    // --------JUMPING-----------------------
    private void Jump()
    {
        //jumpmode += 1;
        exitingSlope = true;

        // reset y velocity so you always jump the same height
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //push the player up with jumpForce
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
       
    }
    private void ResetJump()
    {
        jumpmode = 0;
        readyToJump = true;
        exitingSlope = false;
    }
    // --------SLIDING-----------------------
    private void StartSlide()
    {
        sliding = true;
        
        //when the player crouches, they shrink on the y
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        //the player shrinks to center, so apply force to get low quickly
        rb.AddForce(orientation.forward * (moveSpeed / 2), ForceMode.Impulse);

        slideTimer = maxSlideTime;

    }
    private void SlidingMovement()
    {
        if (grounded)
        {            
            slidesoundCounter ++;
            if (slidesoundCounter == 1)
            {
                playerAudio.PlaySlide();
            }
        }

        if (rb.velocity.magnitude *2 <= 1)
        {
            playerAudio.playersound.Stop();
        }

        //normal sliding
        if (!OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(slideDirection.normalized * slideForce, ForceMode.Impulse);

            //slideTimer -= Time.deltaTime;
        }

        //slope sliding
        else
        {
            rb.AddForce(GetSlopeMoveDirection(slideDirection) * slideForce, ForceMode.Impulse);
        }

        if (slideTimer <= 0)
        {
            StopSlide();
        }

    }
    private void StopSlide()
    {
        slidesoundCounter = 0;
        playerAudio.playersound.Stop();
        sliding = false;
        //return the player to normal size when key is released
        //"normal size" is collected on Start()
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }
    // --------WALLRUN----------------------
    private void CheckForWall()
    {
        //to check the right wall, fire a raycast from the player orientation, to the right at a distance we set.
        //We only want results from out "whatIsWall" layer mask, and output the result as "rightWallhit"
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        //do the same for the left
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }
    private void StartWallRun()
    {
        wallrunning = true;
    }
    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        jumpDirection = wallNormal;
        if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        //forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);


        //push player into wall
        if(!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        {
            rb.AddForce(-wallNormal * 50, ForceMode.Force);
        }
        
    }
    private void StopWallRun()
    {
        wallrunning = false;
        jumpDirection = transform.up; 
    }


}
