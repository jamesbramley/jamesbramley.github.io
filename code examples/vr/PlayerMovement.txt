using Valve.VR;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;

public class PlayerMovement : MonoBehaviour
{
    private GameObject cameraRig;
    private GameObject leftController;
    private GameObject rightController;
    private Vector2 touchpadValue;
    private Vector2 rightTouchpadValue;

    private float maxDistance = 100f;

    private bool moving;
    private bool falling;

    public bool instant; // Whether or not instant movement is happening.

    private LineRenderer teleportLine;

    private float differenceAngle; // The angle between the headset's current and original position.

    public SteamVR_Action_Vector2 touchpadAction;
    public bool IgnoringTerrain { get; set; }

    public float rotationValue = 30f;
    
    void Start()
    {
        cameraRig = GameObject.Find("[CameraRig]");
        touchpadAction = SteamVR_Input._default.inActions.MovementDirection;
        leftController = GameObject.Find("Controller (left)");
        rightController = GameObject.Find("Controller (right)");
        teleportLine = GameObject.Find("Teleport Line").GetComponent<LineRenderer>();
        IgnoringTerrain = false;
    }

    void Update()
    {
        // Get the position of the left thumb on the touchpad.
        touchpadValue = touchpadAction.GetAxis(SteamVR_Input_Sources.LeftHand);
        rightTouchpadValue = touchpadAction.GetAxis(SteamVR_Input_Sources.RightHand);
        
        teleportLine.SetPosition(0, leftController.transform.position);
        teleportLine.SetPosition(1, leftController.transform.position + leftController.transform.forward*10f);
        
        MoveToFloor();
        if (SteamVR_Input._default.inActions.Teleport.GetStateUp(SteamVR_Input_Sources.LeftHand) && touchpadValue.y >= 0.3f)
        {
            var val = GetPositionToMoveTo();
            if (!moving && !IsPhasingThroughWall())
            {
                StartCoroutine(Move(val));
            }
            
        }

        if (SteamVR_Input._default.inActions.Teleport.GetStateDown(SteamVR_Input_Sources.RightHand) && rightTouchpadValue.x < 0)
        {
            cameraRig.GetComponent<SteamVR_PlayArea>().transform.Rotate(0, -rotationValue, 0);
            leftController.GetComponent<Controller>().offset -= rotationValue;
            rightController.GetComponent<Controller>().offset -= rotationValue;
        }
        else if (SteamVR_Input._default.inActions.Teleport.GetStateDown(SteamVR_Input_Sources.RightHand) && rightTouchpadValue.x > 0 )
        {
            cameraRig.GetComponent<SteamVR_PlayArea>().transform.Rotate(0, rotationValue, 0);
            leftController.GetComponent<Controller>().offset += rotationValue;
            rightController.GetComponent<Controller>().offset += rotationValue;

        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            transform.position = GameObject.Find("UndergroundReset").transform.position;
            StopAllCoroutines();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            transform.position = GameObject.Find("StageReset").transform.position;
            StopAllCoroutines();

        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            transform.position = GameObject.Find("TutorialReset").transform.position;
            StopAllCoroutines();

        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            transform.position = GameObject.Find("FinalReset").transform.position;
            StopAllCoroutines();
        }
        
    }

    private void MoveToFloor()
    {
        RaycastHit hit;

        if (IgnoringTerrain)
        {
            Physics.Raycast(cameraRig.transform.position + new Vector3(0, 0.3f, 0), -cameraRig.transform.up, out hit, 
                Mathf.Infinity, LayerMask.GetMask("Default"));
            
            if (cameraRig.transform.position.y - hit.point.y > 1)
            {
                StartCoroutine(MoveToFloorCoroutine(hit));
            }
        }
        else
        {
            Physics.Raycast(cameraRig.transform.position + new Vector3(0, 0.3f, 0), -cameraRig.transform.up, out hit);
                    
            if (cameraRig.transform.position.y - hit.point.y > 1)
            {
                StartCoroutine(MoveToFloorCoroutine(hit));
            }
        }
    }

    IEnumerator MoveToFloorCoroutine(RaycastHit hit)
    {
        var terminalVelocity = 4f;
        var velocity = Vector3.zero;
        var acceleration = 1f;
        var maxClimbHeight = 0.15f; // How high you can go up before you're considered to be falling.
        if (cameraRig.transform.position.y - hit.point.y > maxClimbHeight)
        {
            falling = true;
        }
        while (falling)
        {
            if (cameraRig.transform.position.y - hit.point.y <= 0.1)
            {
                falling = false;
            }

            if (velocity.magnitude < terminalVelocity)
            {
                velocity += (acceleration * -cameraRig.transform.up) * Time.deltaTime;
                cameraRig.transform.position += velocity * Time.deltaTime;
            }
            
            yield return null;
        }

        yield return null;
    }

    public Vector3 GetPositionToMoveTo()
    {
        var controllerForward = leftController.GetComponent<SteamVR_Behaviour_Pose>().transform.forward;
        RaycastHit hit;
        if (IgnoringTerrain)
        {
            if (Physics.Raycast(leftController.transform.position,
                leftController.transform.forward, out hit, 10, LayerMask.GetMask("Default")))
            {
                // Stops the player from climbing really high things
                if (!(hit.point.y - transform.position.y > 1 && Vector2DDistance(hit.point, transform.position) < 7))
                {
                    return hit.point;
                }
            
            }
        }
        else if (Physics.Raycast(leftController.transform.position,
            leftController.transform.forward, out hit, 10))
        {
            
            // Stops the player from climbing really high things
            if (!(hit.point.y - transform.position.y > 1 && Vector2DDistance(hit.point, transform.position) < 7))
            {
                return hit.point;
            }
            
        }
        
        return transform.position;

    }

    IEnumerator Move(Vector3 target)
    {
        var targetVector = new Vector3(target.x, transform.position.y, target.z); // The vector to move to. Y is the same as camera rig.

        if (instant)
        {
            transform.position = target;
            yield break;
        }
        
        moving = true;
        var fallingCountFrames = 0; // Number of frames you must be above thin air before you start falling.

        while (Vector2DDistance(transform.position, targetVector) > 0.1f )
        {
            if (falling)
            {
                targetVector.y = transform.position.y; // If falling make sure don't conflict coroutines.
            }
            else
            {
                // Raycast to find the ground, and move the user to the ground as they move across it.
                RaycastHit hit;
                if (Physics.Raycast(cameraRig.transform.position + new Vector3(0, 0.3f, 0), -cameraRig.transform.up, out hit))
                {
                    fallingCountFrames += 1;
                    if (!(hit.point.y - transform.position.y > 2))
                    {
                        transform.position = new Vector3(transform.position.x, hit.point.y + 0.3f, transform.position.z);
                    }
                }
            }
            
            transform.position = Vector3.MoveTowards(transform.position, targetVector , 0.14f );
            yield return null;
        }

        moving = false;
        yield return null;
    }
    
    // Calculate the flat difference between 2 vectors.
    private float Vector2DDistance (Vector3 vector1, Vector3 vector2)
    {
        float xDiff = vector1.x - vector2.x;
        float zDiff = vector1.z - vector2.z;
        return Mathf.Sqrt((xDiff * xDiff) + (zDiff * zDiff));
    }

    // Raycast from player position to hit to ensure it isn't through a wall.
    private bool IsPhasingThroughWall()
    {
        RaycastHit hit;
        var target = GetPositionToMoveTo();
        var direction = (target + new Vector3(0, 0.3f, 0)) - cameraRig.transform.position + new Vector3(0, 0.3f, 0);
        var distance = Vector3.Distance(target, cameraRig.transform.position);
        var didHit = Physics.Raycast(cameraRig.transform.position + new Vector3(0, 0.3f, 0), direction, out hit, distance);

        try
        {
            if (hit.collider.name == "Terrain")
            {
                return false;
            }
        }
        catch (Exception e)
        {
            // The collider isn't terrain.
        }
        
        return didHit;
    }
}
