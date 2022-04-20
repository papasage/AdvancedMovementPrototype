using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Movement")]
    private float moveSpeed;
    public enum MovementState { walking, sprinting, air, }
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

    


    [Header("GroundCheck")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Orientation Point on Player")]
    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    [Header("Speedometer")]
    public Text SpeedText;

    void Start()
    {
        //on start, we will find the player's Rigidbody component
        rb = GetComponent<Rigidbody>();
        //we freeze the rotations so that it doesn't fall over. we are controlling every move the player makes
        rb.freezeRotation = true;
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
    }

    private void StateHandler()
    {
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

        //move our rigidbody in the move direction * moveSpeed
        // vector3.normalized will return the same direction, but with a length of 1.0

        //on the ground apply a normal force
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        //in the air apply a modified force with airMultiplier
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
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
