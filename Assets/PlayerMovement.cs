using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;

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

        //handle drag
        if (grounded)
            rb.drag = groundDrag;
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
    }

    private void MovePlayer()
    {
        //movement direction is FORWARD/BACKWARD movement + LEFT/RIGHT movement
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //move our rigidbody in the move direction * moveSpeed
        // vector3.normalized will return the same direction, but with a length of 1.0
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        //flatVel is only the X and Z movement speed
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //display speed on HUD
        SpeedText.text = "Speed:"; // flatVel.magnitude.ToString;

        //if it is greater than our moveSpeed, then recalculate what it should be and apply
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }
}
