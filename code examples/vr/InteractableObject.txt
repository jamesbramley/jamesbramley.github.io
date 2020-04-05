using System.Collections;
using System.Xml.Xsl;
using UnityEngine;
using Valve.Newtonsoft.Json.Utilities;
using Valve.VR;

public class InteractableObject : MonoBehaviour
{
    public bool pickedUp;
    public bool inRange;
    public ObjectTypes.ObjectTypeID objectType;
    private Rigidbody rigidBody;
    private Material material;
    private AudioSource audioPlayer;

    public bool justImpacted;

    private Color baseGlowColor = Color.red;
    private bool glowing;

    public bool fixX;
    public bool fixY;
    public bool fixZ;
    public bool fixRot;

    private float movementSpeed = 0.1f;
    private float weight;

    public bool isNotKinematicWhenPickedUp; // Does it phase through objects when picked up?

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        material = GetComponent<Renderer>().material;
        material.SetColor("_EmissionColor", baseGlowColor);

        gameObject.AddComponent<AudioSource>();
        audioPlayer = GetComponent<AudioSource>();
        audioPlayer.maxDistance = 3f;
        audioPlayer.spatialBlend = 1;
        audioPlayer.playOnAwake = false;

        weight = rigidBody.mass;
    }

    private void Update()
    {
        if (!pickedUp && inRange)
        {
            Glow();
        }
        else
        {
            StopGlow();
        }

        if (fixX)
        {
            gameObject.GetComponent<Rigidbody>().constraints = gameObject.GetComponent<Rigidbody>().constraints | RigidbodyConstraints.FreezePositionX;
        }
        
        if (fixY)
        {
            gameObject.GetComponent<Rigidbody>().constraints = gameObject.GetComponent<Rigidbody>().constraints | RigidbodyConstraints.FreezePositionY;
        }
        
        if (fixZ)
        {
            gameObject.GetComponent<Rigidbody>().constraints = gameObject.GetComponent<Rigidbody>().constraints | RigidbodyConstraints.FreezePositionZ;
        }

        if (fixRot)
        {
            gameObject.GetComponent<Rigidbody>().constraints = gameObject.GetComponent<Rigidbody>().constraints | RigidbodyConstraints.FreezeRotation;
        }

        if (justImpacted)
        {
            audioPlayer.PlayOneShot(AudioLoader.LoadAudio(GetAudioType()));
        }
    }

    private Audio.AudioClipID GetAudioType()
    {
        switch (objectType)
        {
                case ObjectTypes.ObjectTypeID.Sword:
                case ObjectTypes.ObjectTypeID.Halberd:
                case ObjectTypes.ObjectTypeID.Shield:
                    return Audio.AudioClipID.MetalHit;
                
                default: return Audio.AudioClipID.RockHit;

        }
    }

    public void Pickup(GameObject controller)
    {
        /*
        if (!isNotKinematicWhenPickedUp)
        {
            rigidBody.isKinematic = true; // Ignore physics when picked up.
        }
        else
        {
            movementSpeed = 0.05f;
            rigidBody.useGravity = false;
        }
        
        */

        rigidBody.useGravity = false;
        
        StopGlow();
        var fixedJoint = controller.GetComponent<FixedJoint>();
        fixedJoint.connectedBody = rigidBody;
        
        pickedUp = true; // Set the object's status to 'pickedUp'.
        StartCoroutine(WeightPickup(controller));
    }

    public void Drop(GameObject controller)
    {
        var behaviourPose = controller.GetComponent<SteamVR_Behaviour_Pose>();

        var fixedJoint = controller.GetComponent<FixedJoint>();
        fixedJoint.connectedBody = null;
        
        pickedUp = false;
        rigidBody.isKinematic = false;
        var velocity = Quaternion.AngleAxis(controller.GetComponent<Controller>().offset, Vector3.up) *
                       behaviourPose.GetVelocity();
        rigidBody.velocity = velocity;        
        var adjustedAngularVelocity = Quaternion.AngleAxis(controller.GetComponent<Controller>().offset, Vector3.up) *
                                 behaviourPose.GetAngularVelocity();
        rigidBody.angularVelocity = adjustedAngularVelocity;
        rigidBody.useGravity = true;
 
    }

    public void Glow()
    {
        glowing = true;
        material.EnableKeyword("_EMISSION");
        StartCoroutine(GlowEffect());
    }

    public void StopGlow()
    {
        material.DisableKeyword("_EMISSION");
        glowing = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        justImpacted = true;
    }

    private void OnCollisionStay(Collision other)
    {
        justImpacted = false;
    }

    private void OnCollisionExit(Collision other)
    {
        justImpacted = false;
    }

    IEnumerator GlowEffect()
    {
        while (glowing)
        {
            var emission = Mathf.PingPong(Time.time, 0.5f); // Pulsing effect.
            material.SetColor("_EmissionColor", baseGlowColor * Mathf.LinearToGammaSpace(emission));
            yield return null;
        }

        yield return null;
    }

    IEnumerator WeightPickup(GameObject controller)
    {
        var body = controller.GetComponent<SteamVR_Behaviour_Pose>();
        while (pickedUp)
        {
            if (body.GetVelocity().magnitude > GetMaxVelocity(weight))
            {
                Drop(controller);
            }
            yield return null;
        }
        yield return null;
    }

    private float GetMaxVelocity(float weight)
    {
        var max = 6f / weight;
        return max;
    }

    IEnumerator MoveToController(GameObject controller)
    {
        StopGlow();
        var rotationOffset = transform.eulerAngles - controller.transform.eulerAngles;
        
        while (pickedUp)
        {
            var xPos = controller.transform.position.x;
            var yPos = controller.transform.position.y;
            var zPos = controller.transform.position.z;

            var rotation = controller.transform.rotation;
            
            if (fixX)
            {
                xPos = transform.position.x;
            }

            if (fixY)
            {
                yPos = transform.position.y;
            }

            if (fixZ)
            {
                zPos = transform.position.z;
            }

            if (fixRot)
            {
                rotation = transform.rotation;
            }
            
            
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(xPos, yPos, zPos), movementSpeed);
            var euler = controller.transform.eulerAngles + controller.transform.TransformDirection(rotationOffset);
            transform.eulerAngles = euler;
            
            yield return null;
        }

        yield return null;
    }
}
