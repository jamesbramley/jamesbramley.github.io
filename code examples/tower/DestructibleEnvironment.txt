using System.Linq;
using UnityEngine;
using System;

public class DestructibleEnvironment : MonoBehaviour
{
    private Vector2 dimensions;
    protected BoxCollider2D currentCollider;
    protected SpriteRenderer spriteRenderer;

    protected const float AreaDestroyedByExplosion = 0.8f;

    protected const float MinimumSize = 1f; // The minimum size a piece of the environment should be before it's considered too small and is destroyed.
    
    public RoomSide roomSide;

    private void Start()
    {
        currentCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "EnvironmentSides";
        CheckSize();

    }

    private void Update()
    {
    }

    protected virtual void CheckSize()
    {
        if (currentCollider.bounds.size.x < MinimumSize)
        {
            Destroy(gameObject); // Probably play a different destruction animation to normal when this happens.
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.tag.Equals("Explosion"))
        {
            return;
        }
        
        HitByExplosion(collision);
    }
    
    private void HitByExplosion(Collision2D collision)
    {
        var breakPoints = DetermineBreakPoints(collision);
        ContactPoint2D point1;
        
        try
        {
            point1 = breakPoints[0];
        }
        catch (Exception e)
        {
            Debug.Log("NO BREAK POINTS");
            return;
        }
        
        Create2Parts(point1.point);
    }

    protected virtual void Create2Parts(Vector2 explosionPoint)
    {
        var floorCentre = transform.position;
        var leftMostPoint = (floorCentre - currentCollider.bounds.size / 2f).x;
        var rightMostPoint = (floorCentre + currentCollider.bounds.size / 2f).x;
        var hasBody = GetComponent<Rigidbody2D>() != null;

        if (explosionPoint.x - AreaDestroyedByExplosion >= leftMostPoint)
        {
            var floor1EndPos = new Vector2(explosionPoint.x - AreaDestroyedByExplosion, transform.position.y);
            var floor1StartPos = new Vector2(leftMostPoint, transform.position.y);
            var xPos1 = (floor1StartPos.x + floor1EndPos.x) / 2f;
            var newFloor1 = Instantiate(Resources.Load<GameObject>("prefabs/Floor"));
            newFloor1.transform.position = new Vector2(xPos1, transform.position.y);
            StretchBetween(floor1StartPos, floor1EndPos, newFloor1);
            newFloor1.transform.SetParent(gameObject.transform.parent);
            var size = floor1StartPos - floor1EndPos;
            AttachBodyAtRandom(newFloor1.gameObject, size.x, hasBody);
        }

        if (explosionPoint.x + AreaDestroyedByExplosion <= rightMostPoint)
        {
            var floor2EndPos = new Vector2(rightMostPoint, transform.position.y);
            var floor2StartPos = new Vector2(explosionPoint.x + AreaDestroyedByExplosion, transform.position.y);
            var xPos2 = (floor2StartPos.x + floor2EndPos.x) / 2f;
            var newFloor2 = Instantiate(Resources.Load<GameObject>("prefabs/Floor"));
            newFloor2.transform.position = new Vector2(xPos2, transform.position.y);
            StretchBetween(floor2StartPos, floor2EndPos, newFloor2);
            var size = floor2StartPos.x - floor2EndPos.x;
            newFloor2.transform.SetParent(gameObject.transform.parent);
            AttachBodyAtRandom(newFloor2.gameObject, size, hasBody);
        }
        
        Destroy(gameObject);
    }

    // Randomly decide if a damaged floor becomes affected by gravity or not.
    protected void AttachBodyAtRandom(GameObject floor, float mass, bool guaranteed=false)
    {
        const float chance = 0.25f;
        var rand = new System.Random();
        var randNum = rand.NextDouble();
        mass = Math.Abs(mass)/2;
        if (mass < 1)
        {
            mass = 1f;
        }
        if (randNum <= chance || guaranteed)
        {
            floor.AddComponent<Rigidbody2D>();
            floor.GetComponent<Rigidbody2D>().mass = mass;
            floor.GetComponent<SpriteRenderer>().color = Color.grey;
        }
    }

    protected virtual void StretchBetween(Vector2 point1, Vector2 point2, GameObject floor)
    {
        var spriteSize = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
      
        var scale = transform.localScale;
        scale.x = (point1.x - point2.x) / spriteSize;
        floor.transform.localScale = scale;
    }

    private ContactPoint2D[] DetermineBreakPoints(Collision2D collision)
    {
        var contacts = collision.contacts;
        var orderedList = contacts.OrderBy(cp => cp.point.y).ToArray();

        // Gets 2 points in case we need that functionality in future.
        try
        {
            return orderedList.Skip(0).Take(1).ToArray();
        }
        catch (Exception e)
        {
            Debug.Log(e + ": Less than 2 collisions.");
        }

        return null;
    }


    public void DrillAtPoint(Vector2 point) {
        Create2Parts(point);
    }
}
