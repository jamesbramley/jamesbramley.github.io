using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DailyLifeScriptingEngine : ScriptingEngine
{
	
	// Base for all scripting. Will handle how the player talks to characters, interacts with objects etc.
	[FormerlySerializedAs("mainCamera")] public GameObject MainCamera;
	[FormerlySerializedAs("sceneManager")] public SceneManager SceneManager;

	[FormerlySerializedAs("characterSceneObjectCreator")]
	public SceneObjectHandler CharacterSceneObjectCreator;
	
	public SceneChangeRequest NextScene { get; set; }

	private bool dialogueOver = true;
	public bool DialogueOver
	{
		get { return dialogueOver; }
		set { dialogueOver = value; }
	}
	private CharacterID characterInFrame = CharacterID.None;

	private SceneObject currentDialogueObject;

	public void Start()
	{
		_dialogueUiController = new DailyLifeDialogueUiController();
	}
	
	// Starts the dialogue that belongs to a particular object.
	public void StartDialogue(SceneObject inspectedObject)
	{
		if (!uiVisible)
		{
			_dialogueUiController.FadeInSideUI();
			uiVisible = true;
		}
		DialogueOver = false;
		currentDialogueObject = inspectedObject;
		var dialogue = inspectedObject.CurrentDialogue;
		if (dialogue == null) return;

		CurrentDialogue = dialogue;
		
		// Remove old sentences.
		sentences.Clear();
		
		// Add all of the sentences from that object's dialogue.
		foreach (var sentence in dialogue.Sentences)
		{
			sentences.Enqueue(sentence);
		}
		
		// Zoom in on the selected object.
		MainCamera.GetComponent<CameraMovement>().MoveToObject(inspectedObject);
		
		_dialogueUiController.StartDialogue(sentences.First(), false);

		Debug.Log("Initiating " + inspectedObject.Name + " dialogue...");
		GameObject.Find("Cursor").GetComponent<Image>().enabled = false;
		GameObject.Find("Cursor").GetComponent<Cursor>().enabled = false;
		
		var dialogueState = SaveData.Current.GameState.DialogueState;
		dialogueState.Reset();
		dialogueState.ObjectName = inspectedObject.Name;
		dialogueState.ObjectState = inspectedObject.State;
		
		// Display the next sentence on screen.
		DisplayNextSentence();
	}

	protected override void HandleSentence(Sentence sentence)
	{
		base.HandleSentence(sentence);

		var dailyLifeSentence = (DailyLifeSentence) sentence;
		
		var characterChange =
			dailyLifeSentence.characterChanges.Any(); // Is there a character leaving/entering the scene during this sentence?
		var characterSceneAnimation =
			dailyLifeSentence.characterChanges.Any(ch =>
				ch.PlayAnimation); // Is the character enter/leave animation playing during this sentence?
		
		if (SceneManager.CharacterExistsInCurrentScene(dailyLifeSentence.character))
		{
			MainCamera.GetComponent<CameraMovement>()
				.MoveToCharacter(dailyLifeSentence.character); // Camera movement during dialogue.

			// If there is a character in the frame and the character currently speaking is not that character...
			if (characterInFrame != CharacterID.None && CurrentSentence.character != characterInFrame)
			{
				// Fade that character in as they are not being spoken to but can still be seen.
				CharacterSceneObjectCreator.FadeObjectIn(SceneManager.CurrentScene.GetCharacterObject(characterInFrame));
			}

			var characterObject = SceneManager.CurrentScene.GetCharacterObject(dailyLifeSentence.character);
			characterInFrame = dailyLifeSentence.character;

			// If the character is visible and no enter/leave animation is playing...
			if (characterObject.Visible && !characterSceneAnimation)
			{
				CharacterSceneObjectCreator
					.FadeObjectOut(characterObject); // Fade out the character as we are talking to them.
			}

			// If a character is leaving/entering fade in the character we're talking to.
			if (characterChange)
			{
				CharacterSceneObjectCreator.FadeObjectIn(characterObject);
			}
		}
		
		// Sets the next scene if there is a scene change request. The scene will be visited at the end of the dialogue.
		if (dailyLifeSentence.sceneChange.NextScene != SceneID.None)
		{
			NextScene = dailyLifeSentence.sceneChange;
		}
		
		// If a character is leaving/entering and the animation is playing, hide the UI and zoom out.
		if (characterChange)
		{
			if (characterSceneAnimation)
			{
				MainCamera.GetComponent<CameraMovement>().ReturnToOriginalPosition();
				_dialogueUiController.FadeOutSideUI();
				uiVisible = false;
			}
		}
		
		var currentSectionState = SaveData.Current.GameState.CurrentSectionState;

		// Apply any state changes
		foreach (var stateChangeRequest in dailyLifeSentence.stateChanges)
		{
			if (stateChangeRequest.IsInternal())
			{
				var affectedObject = stateChangeRequest.AffectsCurrentObject()
					? currentDialogueObject
					: SceneManager.CurrentScene.AllSceneObjects.First(x =>
						x.Name == stateChangeRequest.ObjectName);
				affectedObject.State = stateChangeRequest.State;
				currentSectionState.CurrentSceneState.SetObjectState(affectedObject.Name, affectedObject.State);
			}
			else
			{
				currentSectionState.GetSceneObjectStateData(stateChangeRequest.SceneId)
					.SetObjectState(stateChangeRequest.ObjectName, stateChangeRequest.State);
			}
		}

		foreach (var characterChangeRequest in dailyLifeSentence.characterChanges)
		{
			var affectedObject =
				SceneManager.CurrentScene.CharacterObjects.First(x => x.Name == characterChangeRequest.CharacterName);

			if (characterChangeRequest.Create)
			{
				if (!affectedObject.ExistsInScene)
				{
					affectedObject.ObjectInScene = CharacterSceneObjectCreator.CreateObjectInScene(
						ResourceBank.Instance.GetSprite(affectedObject.Sprite),
						affectedObject.Position,
						affectedObject.Scale,
						true
					);
				}
			}
			else
			{
				if (affectedObject.ExistsInScene)
				{
					CharacterSceneObjectCreator.RemoveObjectFromScene(affectedObject);
				}
			}

			currentSectionState.CurrentSceneState.SetCharacterExists(affectedObject.Name, characterChangeRequest.Create);
		}
	}
	
	public override void EndDialogue()
	{
		currentDialogueObject = null;
		DialogueOver = true;
		_dialogueUiController.EndDialogue(OnDialogueEndAnimationComplete, false);

		Debug.Log("Ending dialogue...");
		
		CharacterSceneObjectCreator.FadeObjectIn(SceneManager.CurrentScene.GetCharacterObject(characterInFrame));
		
		CurrentSentence = null;
		
		SfxManager.PlaySound(SfxID.DialogueEnd); // Play the dialogue advance sound effect.
		
		MainCamera.GetComponent<CameraMovement>().ReturnToOriginalPosition();

		// If you are rewarded monocoins for this dialogue...
		if (CurrentDialogue.monocoins > 0)
		{
			GetDialogueMonocoins();
		}

		CurrentDialogue = null;

	}

	public override bool CanAdvanceDialogue()
	{
		return UiController.CanAdvance() && !MainCamera.GetComponent<CameraMovement>().Zooming;
	}
	
	private void BeginDisplay()
	{
		
	}
	
	public void OnDialogueEndAnimationComplete()
	{
		// Re-enable the cursor.
		GameObject.Find("Cursor").GetComponent<Cursor>().enabled = true;
		GameObject.Find("Cursor").GetComponent<Image>().enabled = true;

		if (NextScene == null) return;
		
		if (NextScene.ChangesSection())
		{
			SaveData.Current.GameState.ChangeSection(NextScene.NextSection);
		}
		SceneManager.TriggerSceneChange(NextScene);
	}


	public void RestoreDialogueFromState(DialogueState dialogueState)
	{
		var activeObject = SceneManager.CurrentScene.AllSceneObjects.Find(x => x.Name.Equals(dialogueState.ObjectName));

		var newSentences = new List<Sentence>(activeObject.GetDialogue(dialogueState.ObjectState).Sentences);

		newSentences.RemoveRange(0, dialogueState.SentenceIndex - 1);
		
		DialogueOver = false;
		
		sentences.Clear();
		
		foreach (var sentence in newSentences)
		{
			sentences.Enqueue(sentence);
		}

		CurrentTrack = sentences.First().track;
		
		MainCamera.GetComponent<CameraMovement>().MoveToObjectInstant(activeObject);
		MainCamera.GetComponent<CameraMovement>().ZoomInInstant();
		
		_dialogueUiController.StartDialogue(newSentences.First(), true);

		GameObject.Find("Cursor").GetComponent<Image>().enabled = false;
		GameObject.Find("Cursor").GetComponent<Cursor>().enabled = false;
		
		DisplayNextSentence();
	}

	public void AbortDialogue()
	{
		currentDialogueObject = null;
		_dialogueUiController.AbortAllAnimations();
		_dialogueUiController.EndDialogue(OnDialogueEndAnimationComplete, true);
	}
	
	public override void RestoreGameState()
	{
		SceneManager.TriggerSceneChange(new SceneChangeRequest(SaveData.Current.GameState.CurrentSectionState.CurrentScene));
		AbortDialogue();
		ChangeMusic(new MusicChangeRequest(SaveData.Current.GameState.CurrentMusic));
		BacklogMenu.RestoreFromHistory(SaveData.Current.GameState.DialogueState.History);
		if (SaveData.Current.GameState.DialogueState.IsDialogueActive)
		{
			RestoreDialogueFromState(SaveData.Current.GameState.DialogueState);
		}
		else
		{
			MainCamera.GetComponent<CameraMovement>().ReturnToOriginalPositionInstant();
			MainCamera.GetComponent<CameraMovement>().ZoomOutInstant();
		}
	}
}
