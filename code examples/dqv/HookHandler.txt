
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HookHandler : MonoBehaviour
{
    private Player player;
    private GrapplingHook activeHook;
    private LineRenderer hookString;
    public HingeJoint2D Joint { get; set; }
    public bool Hooked { get; set; }

    private const float MaxGrappleDistance = 3.5f;
    private const float GrappleBufferDistance = 1.5f;

    private HookPoint lastClosestHookPoint = null;

    private void Awake()
    {
        hookString = GetComponent<LineRenderer>();
        Joint = GetComponent<HingeJoint2D>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
        Joint.enabled = false;
    }

    private void Update()
    {
        DisplayClosestHook();

        if (KeyPressedOnce(ControlID.Grapple) && lastClosestHookPoint != null && player.HasControl)
        {
            UseHook();
        }

        if (KeyReleased(ControlID.Grapple))
        {
            if (Hooked)
            {
                HookOff();
                Joint.enabled = false;
            }
            
        }
        
    }

    private void DisplayClosestHook()
    {
        var hook = GetNearestHookPoint();
        if (hook == lastClosestHookPoint)
        {
           return;
        }
        
        if (lastClosestHookPoint != null)
        {
            lastClosestHookPoint.Remove();
        }
        lastClosestHookPoint = hook;
        if (hook != null)
        {
            hook.PlayHighlightAnimation();
        }
    }
    
    private void UseHook()
    {
        if (activeHook != null)
        {
            HookOff();
        }
        var hook = Resources.Load<GrapplingHook>("prefabs/Hook");
        var hookObject = Instantiate(hook);
        activeHook = hookObject;
        activeHook.HookHandler = this;
        hookObject.transform.position = transform.position;
        var nearestHookPoint = GetNearestHookPoint();
        if (nearestHookPoint)
        {
            hookObject.HookOnToTarget(nearestHookPoint);
        }
    }

    public void HookOn(Vector2 position)
    {
        Hooked = true;
        Joint.enabled = true;
        InstantiateRope(position);
    }

    public void HookOff()
    {
        activeHook.HookOff();
        var nodes = FindObjectsOfType<RopeNode>();
        foreach (var ropeNode in nodes)
        {
            Destroy(ropeNode.gameObject);
        }
    }

    private void InstantiateRope(Vector2 position)
    {
        const float nodeInterval = 1f; // Distance between nodes.
        Vector2 target = transform.position;
        var numberOfNodes = Vector2.Distance(position, target + Joint.anchor) / nodeInterval;
        var nodes = new Queue<Rigidbody2D>();
        nodes.Enqueue(activeHook.GetComponent<Rigidbody2D>());
        while (Vector2.Distance(position, target + Joint.anchor) > 0.1f)
        {
            position = Vector2.MoveTowards(position, target + Joint.anchor, numberOfNodes*Time.deltaTime*10);
            var newNode = Resources.Load<RopeNode>("prefabs/RopeNode");
            var nodeObject = Instantiate(newNode);
            nodeObject.transform.position = position;
            var previousNode = nodes.Dequeue();
            nodeObject.HingeJoint2D.connectedBody = previousNode;
            nodeObject.PreviousNode = previousNode.gameObject;
            nodes.Enqueue(nodeObject.GetComponent<Rigidbody2D>());
        }

        var lastNode = nodes.Dequeue();
        lastNode.velocity = player.GetComponent<Rigidbody2D>().velocity; // Make grapple smoother.
        Joint.connectedBody = lastNode;
    }
    
    private bool CheckHookPointValid(HookPoint hookPoint)
    {
        switch (player.PlayerMovement.CurrentDirection)
        {
            case Direction.Right:
                return hookPoint.transform.position.x > transform.position.x - GrappleBufferDistance;
            case Direction.Left:
                return hookPoint.transform.position.x < transform.position.x + GrappleBufferDistance;
            
            default: return false;
        }
    }
    
    private HookPoint GetNearestHookPoint()
    {
        var hookPoints = player.CurrentStage.HookPoints;
        
        var closestDistance = Vector2.Distance(transform.position, hookPoints[0].transform.position);
        var closestHook = hookPoints[0];
        
        foreach (var hookPoint in hookPoints)
        {
            var newDistance = Vector2.Distance(transform.position, hookPoint.transform.position);
            
            if (newDistance < closestDistance)
            {
                closestDistance = newDistance;
                closestHook = hookPoint;
            }
        }

        if (CheckHookPointValid(closestHook) && closestDistance < MaxGrappleDistance)
        {
            return closestHook;
        }

        return null;
    }
    
    private bool KeyPressed(ControlID controlId)
    {
        return Input.GetButton(GetKeyCode(controlId));
    }
    
    private bool KeyReleased(ControlID controlId)
    {
        return Input.GetButtonUp(GetKeyCode(controlId));
    }
    
    private bool KeyPressedOnce(ControlID controlId)
    {
        return Input.GetButtonDown(GetKeyCode(controlId));
    }
    
    private string GetKeyCode(ControlID controlId)
    {
        return Controls.GetKeyCode(controlId);
    }
}
