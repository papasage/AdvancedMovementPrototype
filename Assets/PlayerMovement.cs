using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    [Header("Movement")]
    private float moveSpeed;
    public enum MovementState { walking, sprinting, crouching, air, }
    public MovementState state;
    public float walkSpeed;
    public float sprintSpeed;
    public float wallrunSpeed;
    public float groundDrag;
    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public bool readyToJump;
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;
    //--------------------------------------------------
    [Header("GroundCheck")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    [Header("Orientation Point on Player")]
    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;
    //-------------------------------------------------
    [Header("Speedometer")]
    public Text SpeedText;

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
        if (grounded)
        {
            rb.drag = groundDrag;
            ResetJump();
        }
            
        else
            rb.drag = 0;

        
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        //this function is collecting keyboard input data
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
        if (Input.GetKeyDown(crouchKey))
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
    }

    private void StateHandler()
    {
        //Mode-Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        //Mode-Sprinting
        if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        //Mode-Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        //Mode-Air
        else
        {
            state = MovementState.air;
        }
    }
    private void MovePlayer()
    {
        //movement direction is FORWARD/BACKWARD movement + LEFT/RIGHT movement
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if(OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10f, ForceMode.Force);

            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f);
            }
        }

        //move our rigidbody in the move direction * moveSpeed
        // vector3.normalized will return the same direction, but with a length of 1.0

        //on the ground apply a normal force
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        //in the air apply a modified force with airMultiplier
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        //turn gravity off while on a slope
        rb.useGravity = !OnSlope();
    }

    private void Jump()
    {
        // reset y velocity so you always jump the same height
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //push the player up with jumpForce
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight *0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void SpeedControl()
    {
        //flatVel is only the X and Z movement speed
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //SPEEDOMETER CODE HERE
        float kilometerConversion = flatVel.magnitude * 2;
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
