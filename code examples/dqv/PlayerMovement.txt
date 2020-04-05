
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float MovementSpeed = 3.5f;
    private const float JumpForce = 300f;
    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;

    private ControlID currentBufferedInput;

    public Direction CurrentDirection { get; set; }
    public bool TouchingGround { get; set; }
    public bool Falling { get; set; }

    private Player player;
    private HookHandler hookHandler;
    private BoxCollider2D boxCollider2D;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponent<Player>();
        hookHandler = GetComponent<HookHandler>();
        CurrentDirection = Direction.Left;
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        if (!player.HasControl)
        {
            rigidBody.velocity = Vector2.zero;
            return;
        }
        HandleMovement();
    }

    private void Update()
    {
        player.Running = (TouchingGround && Math.Abs(Input.GetAxis("Horizontal")) > 0.1 && Math.Abs(rigidBody.velocity.x) > 0.1);
        player.Animator.SetFloat("ySpeed", rigidBody.velocity.y);
        TouchingGround = CheckOnGroundRayCast();
        Falling = rigidBody.velocity.y < 0 && !TouchingGround;
    }

    private bool CheckOnGroundRayCast()
    {   
        var hitLeft = Physics2D.Raycast(transform.position - new Vector3(boxCollider2D.bounds.extents.x, 0), Vector2.down, player.BoxCollider.size.y/4f);
        if (hitLeft.collider != null && hitLeft.collider.gameObject.CompareTag("Ground"))
        {
            return true;
        }
        
        var hitRight = Physics2D.Raycast(transform.position + new Vector3(boxCollider2D.bounds.extents.x, 0), Vector2.down, player.BoxCollider.size.y/4f);
        if (hitRight.collider != null && hitRight.collider.gameObject.CompareTag("Ground"))
        {
            return true;
        }
        
        var hitMiddle = Physics2D.Raycast(transform.position, Vector2.down, player.BoxCollider.size.y/4f);
        return hitMiddle.collider != null && hitMiddle.collider.gameObject.CompareTag("Ground");
    }

    private void HandleMovement()
    {
        var movementVector = new Vector3(0, rigidBody.velocity.y, 0);
        
        if (hookHandler.Hooked)
        {
            movementVector.x = rigidBody.velocity.x;
        }
        
        if (Math.Abs(movementVector.x) < MovementSpeed)
        {
            var xMovement = Input.GetAxisRaw(GetButtonString(ControlID.ControllerX)); // Raw to prevent drift.
            if (xMovement < 0)
            {
                movementVector.x -= MovementSpeed;
                CurrentDirection = Direction.Left;
            }

            if (xMovement > 0)
            {
                movementVector.x += MovementSpeed;
                CurrentDirection = Direction.Right;
            }
        }

        spriteRenderer.flipX = CurrentDirection == Direction.Right;

        rigidBody.velocity = movementVector;
        
        if (KeyPressedOnce(ControlID.Jump))
        {
            if (TouchingGround)
            {
                player.Animator.SetBool("Jumping", true);
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0);
                rigidBody.AddRelativeForce(new Vector3(0, JumpForce, 0));
                player.SFXHandler.PlaySFX(SoundEffectID.Jump);
            }
        }
        else if (TouchingGround && rigidBody.velocity.y <= 0)
        {
            player.Animator.SetBool("Jumping", false);
            
        }

    }

    private void BufferInput() // Buffer player input to create 
    {
        const float bufferDelay = 0.5f; // Seconds
        
    }

    private void ExecuteBufferedInput()
    {
        
    }

    private bool KeyPressed(ControlID controlId)
    {
        return Input.GetButton(GetButtonString(controlId));
    }
    
    private bool KeyReleased(ControlID controlId)
    {
        return Input.GetButtonUp(GetButtonString(controlId));
    }
    
    private bool KeyPressedOnce(ControlID controlId)
    {
        return Input.GetButtonDown(GetButtonString(controlId));
    }

    private string GetButtonString(ControlID controlId)
    {
        return Controls.GetKeyCode(controlId);
    }
}
