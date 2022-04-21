using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    // ----------------------------------------------------------------------------------------------------------------------------
    // VARIABLES
    // ----------------------------------------------------------------------------------------------------------------------------
    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode slideKey = KeyCode.LeftControl;
    [Header("Movement")]
    private float moveSpeed;
    public enum MovementState { walking, sprinting, crouching, sliding, air, }
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
    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    bool exitingSlope;
    [Header("Speedometer")]
    public Text SpeedText;
    [Header("Additional Variables")]
    public float playerHeight;
    public Transform orientation;
    public LayerMask whatIsGround;
    bool grounded;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    // ----------------------------------------------------------------------------------------------------------------------------
    // START / UPDATE / FIXED UPDATE
    // ----------------------------------------------------------------------------------------------------------------------------

    void Start()
    {
        //on start, we will find the player's Rigidbody component
        rb = GetComponent<Rigidbody>();
        //we freeze the rotations so that it doesn't fall over. we are controlling every move the player makes
        rb.freezeRotation = true;

        //save the starting player height for returning from a crouch
        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

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
            rb.drag = 0;

        
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

            Jump();

            //Jump will repeat if key is held, but at a jumpCooldown interval.
            //Invoke(nameof(ResetJump), jumpCooldown);
        }
        //CROUCH START
        if (Input.GetKeyDown(crouchKey) && moveSpeed < 4)
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
        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && moveSpeed >= 4)
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
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }

        //Mode-Walking
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
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
    private void Jump()
    {
        exitingSlope = true;

        // reset y velocity so you always jump the same height
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //push the player up with jumpForce
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

    }
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    } 
    private void StartSlide()
    {
        sliding = true;
        
        //when the player crouches, they shrink on the y
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        //the player shrinks to center, so apply force to get low quickly
        rb.AddForce(orientation.forward * moveSpeed, ForceMode.Impulse);

        slideTimer = maxSlideTime;

    }
    private void SlidingMovement()
    {
        

        //normal sliding
        if(!OnSlope() || rb.velocity.y > -0.1f)
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
        sliding = false;
        //return the player to normal size when key is released
        //"normal size" is collected on Start()
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }



}
