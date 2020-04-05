using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Valve.VR;

public class Controller : MonoBehaviour
{
	private const float PickupVibrationTime = 0.3f; // Amount of haptic feedback for picking up an object.

	private GameObject trackedObject;
	private GameObject touchingTrackedObject;

	private InteractionOptions interactionOptions;

	private GameObject rightHand; // Only assign this if this is the left hand.
	private GameObject leftHand; // Only assign if this is right hand;

	public PlayerEffectController playerEffectController;
	
	public bool IsLeftHand { get; set; }
	public bool HoldingItem { get; set; }

	public float offset = 0;

	private float DistanceToPickup;

	public bool useColliderForPickup;
	private bool touchingInteractableObject;
	private PlayerMovement playerMovement;

	// Use this for initialisation
	void Start ()
	{
		playerMovement = GetComponentInParent<PlayerMovement>();
		IsLeftHand = (gameObject.name == "Controller (left)");
		if (IsLeftHand)
		{
			rightHand = GameObject.Find("Controller (right)");
		}
		else
		{
			leftHand = GameObject.Find("Controller (left)");
		}
		
		interactionOptions = new InteractionOptions();
		DistanceToPickup = 0.65f;
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (IsLeftHand)
		{
			if (SteamVR_Input._default.inActions.GrabGrip.GetStateDown(SteamVR_Input_Sources.LeftHand))
			{
				if (!playerEffectController.messageDisplay.displaying)
				{
					ToggleMovementMethod();
					playerEffectController.DisplayMessage(GetPickupDistanceMessage());
				}
				
			}
		}
		else
		{
			if (SteamVR_Input._default.inActions.GrabGrip.GetStateDown(SteamVR_Input_Sources.RightHand))
			{
				if (!playerEffectController.messageDisplay.displaying)
				{
					TogglePickupDistance();
					playerEffectController.DisplayMessage(GetPickupDistanceMessage());
				}
				
			}
		}
				
		try
		{
			if (!useColliderForPickup)
			{
				trackedObject = FindClosestInteractableObject();
                
				// This takes into account the right hand so no need to do it twice.
				if (IsLeftHand)
				{
					// Update the 'inRange' variable on each interactable object.
					foreach (var objectInScene in GameObject.FindGameObjectsWithTag("Interactable"))
					{
						
						objectInScene.GetComponent<InteractableObject>().inRange =
										(Vector3.Distance(objectInScene.transform.position, transform.position) < DistanceToPickup);
						
						// If it wasn't in range of the left hand it might be in range of the right hand.
						if (!objectInScene.GetComponent<InteractableObject>().inRange)
						{
							objectInScene.GetComponent<InteractableObject>().inRange =
								(Vector3.Distance(objectInScene.transform.position, rightHand.transform.position) < DistanceToPickup);
						}
					}
				}
			}
			
		}
		catch (Exception e)
		{
			// There are no interactable objects in the level.
			Debug.Log("No interactable objects in level: " + e);
		}
		
		// Handles which hand the object should move to. Not a fan of this implementation so may have to change.

		if (IsLeftHand)
		{
			if (SteamVR_Input._default.inActions.Pickup.GetStateDown(SteamVR_Input_Sources.LeftHand))
			{
				PickupClosestItem();
			}
			else if (SteamVR_Input._default.inActions.Pickup.GetStateUp(SteamVR_Input_Sources.LeftHand))
			{
				DropCurrentItem();
			}
		}
		else
		{
			if (SteamVR_Input._default.inActions.Pickup.GetStateDown(SteamVR_Input_Sources.RightHand))
			{
				PickupClosestItem();
			}
			else if (SteamVR_Input._default.inActions.Pickup.GetStateUp(SteamVR_Input_Sources.RightHand))
			{
				DropCurrentItem();
			}
		}
	}

	private void ToggleMovementMethod()
	{
		playerMovement.instant = !playerMovement.instant;
		if (playerMovement.instant)
		{
			playerEffectController.DisplayMessage("Teleport");
		}
		else
		{
			playerEffectController.DisplayMessage("Walk");
		}
	}

	private void OnTriggerEnter(Collider interactableObject)
	{
		try
		{
			if (useColliderForPickup)
			{
				touchingInteractableObject = true;
				touchingTrackedObject = interactableObject.gameObject;
				interactableObject.gameObject.GetComponentInParent<InteractableObject>().inRange = true;
			}
		}
		catch (Exception e)
		{
			// The object was destroyed? (I think)
		}
		
	}

	private void OnTriggerExit(Collider interactableObject)
	{
		try
		{
			if (useColliderForPickup)
			{
				touchingInteractableObject = false;
				interactableObject.gameObject.GetComponentInParent<InteractableObject>().inRange = false;
			}
		}
		catch (Exception e)
		{
			// The object was destroyed? (I think)
			Debug.Log("Error OnTriggerExit of controller and interactable object.");
		}
		
	}

	private void PickupClosestItem()
	{
		HoldingItem = true;
		// Check if the object is close enough to get picked up.
		if (!useColliderForPickup)
		{
			if (trackedObject.GetComponent<InteractableObject>().inRange)
			{
				var vibrationStrength = 1f;
				trackedObject.GetComponent<InteractableObject>().Pickup(gameObject);
				
				TriggerVibration(PickupVibrationTime, 1);
			}
		}
		else
		{
			if (touchingInteractableObject)
			{
				var vibrationStrength = 1f;

				try
				{
					touchingTrackedObject.GetComponent<InteractableObject>().Pickup(gameObject);

				}
				catch (Exception e)
				{
					Debug.Log("Couldn't Pickup object: " + e);
					touchingTrackedObject.GetComponentInParent<InteractableObject>().Pickup(gameObject);

				}
				
				TriggerVibration(PickupVibrationTime, 1);
			}
		}
		
	}

	public void TriggerVibration(float duration, float strength)
	{
		// Trigger a vibration in the hand that picked up the object.
		if (IsLeftHand)
		{
			SteamVR_Input._default.outActions.Haptic.Execute(0, duration, 1, strength, SteamVR_Input_Sources.LeftHand);
		}
		else
		{
			SteamVR_Input._default.outActions.Haptic.Execute(0, duration, 1, strength, SteamVR_Input_Sources.RightHand);
		}
	}
	
	public void TogglePickupDistance()
	{
		if (IsLeftHand)
		{
			var rightHandController = rightHand.GetComponent<Controller>();
			DistanceToPickup = (DistanceToPickup == 0.65f) ? 0.3f : 0.65f;
			rightHandController.DistanceToPickup =
				(rightHandController.DistanceToPickup == 0.65f) ? 0.3f : 0.65f;
			useColliderForPickup = !useColliderForPickup;
			rightHandController.useColliderForPickup = !rightHandController.useColliderForPickup;

		}
		else
		{
			var leftHandController = leftHand.GetComponent<Controller>();
			DistanceToPickup = (DistanceToPickup == 0.65f) ? 0.3f : 0.65f;
			leftHandController.DistanceToPickup = (leftHandController.DistanceToPickup == 0.65f) ? 0.3f : 0.65f;
			useColliderForPickup = !useColliderForPickup;
			leftHandController.useColliderForPickup = !leftHandController.useColliderForPickup;

		}

		// Reset the inRange variables of objects you aren't touching.
		if (useColliderForPickup)
		{
			ResetInRangeVariables();
		}
        
	}

	private void ResetInRangeVariables()
	{
		foreach (var objectInScene in GameObject.FindGameObjectsWithTag("Interactable"))
		{

			if (objectInScene != touchingTrackedObject)
			{
				objectInScene.GetComponent<InteractableObject>().inRange = false;
			}
		}
	}

	public string GetPickupDistanceMessage()
	{        
		var type = (DistanceToPickup == 0.65f) ? "Larger" : "Shorter";
        
		var message = "Pickup Distance: " + type;
		return message;
	}

	private void DropCurrentItem()
	{
		try
		{
			HoldingItem = false;
			if (useColliderForPickup)
			{
				try
				{
					touchingTrackedObject.GetComponent<InteractableObject>().Drop(gameObject);
				}
				catch (Exception e)
				{
					Debug.Log("Couldn't Drop Object:" + e);
					touchingTrackedObject.GetComponentInParent<InteractableObject>().Drop(gameObject);
				}
			}
			else
			{
				trackedObject.GetComponent<InteractableObject>().Drop(gameObject);
			}
		}
		catch (Exception e)
		{
			// The object was deestroyed.
			Debug.Log(e);
		}
		
	}

	private GameObject FindClosestInteractableObject()
	{
		GameObject[] interactableGameObjects;
		interactableGameObjects = GameObject.FindGameObjectsWithTag("Interactable");
		var closest = (interactableGameObjects.Length > 0) ? interactableGameObjects[0] : null;
		
		foreach (var interactableGameObject in interactableGameObjects)
		{
			if (Vector3.Distance(interactableGameObject.transform.position, transform.position) < Vector3.Distance(closest.transform.position, transform.position))
			{
				closest = interactableGameObject;
			}
		}

		return closest;
	}
	
}
