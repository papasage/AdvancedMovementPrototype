using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    //This script keeps the camera attached to the player
    //rigidbody camera movement can be glitchy, so we do it this way to keep it clean
   
    //an empty game object that is a child of the player
    public Transform cameraPosition;

    void Update()
    {
        transform.position = cameraPosition.position;
    }
}
