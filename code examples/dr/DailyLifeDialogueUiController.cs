
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.PostProcessing;

/*
 * Interface between ScriptingEngine and the UI. Essentially this is what DialogueBox was originally
 * intended to be, but now it delegates to the DialogueBox and CharacterUi.
 */

public class DailyLifeDialogueUiController : IDialogueUiController
{
    private readonly IDialogueBox _dialogueBox = GameObject.Find("Dialogue Box").GetComponent<DialogueBox>();
    
    public IDialogueBox DialogueBox
    {
        get { return _dialogueBox; }
    }
    
    private readonly MonocoinAnimation _monocoinAnimation = GameObject.Find("Dialogue Box").GetComponent<MonocoinAnimation>();
    private readonly TruthBulletAnimation _truthBulletAnimation = GameObject.Find("Dialogue Box").GetComponent<TruthBulletAnimation>();
    
    private readonly CharacterUi _characterUi = GameObject.Find("Canvas").GetComponent<CharacterUi>();
    private readonly Image _cutsceneImage = GameObject.Find("Cutscene Image").GetComponent<Image>();
    private readonly TalentScreen _talentScreen = GameObject.Find("Talent Screen").GetComponent<TalentScreen>();
    private readonly EmoteManager _emoteManager = GameObject.Find("Character Canvas").GetComponent<EmoteManager>();

    private GameObject mainCamera = GameObject.Find("Main Camera");
    
    private CharacterSpriteID previousCharacterSprite; // Character sprite in prvious sentence.
    private CharacterSpriteID currentCharacterSprite; // Character sprite in current sentence.
    private CharacterSpriteID previousProtagSprite; // Protag sprite in prvious sentence.
    private CharacterSpriteID currentProtagSprite; // Protag sprite in current sentence.

    private bool previousWasQuestion;
    public bool truthBulletVisible; // Whether or not truth bullet is on the screen.

    public void StartDialogue(Sentence sentence, bool instant = false)
    {
        var dailyLifeFirstSentence = (DailyLifeSentence) sentence;
        previousCharacterSprite = CharacterSpriteID.None;
        previousProtagSprite = CharacterSpriteID.None;
        
        DialogueBox.StartDialogue();
        _characterUi.MoveUiIn(dailyLifeFirstSentence.protagSprite != CharacterSpriteID.None, instant);
    }
    
    public void DisplaySentence(Sentence sentence)
    {
        var dailyLifeSentence = (DailyLifeSentence) sentence;
        
        // In an else if so they don't both trigger if both are true. (Even though that shouldn't ever be the case if we're smart.)
        if (dailyLifeSentence.isFlashbackEnd) // End a flashback.
        {
            EndFlashback();
        }
        else if (dailyLifeSentence.isFlashbackStart)
        {
            TriggerFlashback();
        }
        
        UpdateSpriteChanges(dailyLifeSentence);

        DialogueBox.DisplaySentence(dailyLifeSentence);
        
        if (dailyLifeSentence.isTalentScreen)
        {
            currentCharacterSprite = CharacterSpriteID.None; // Hide the character sprite.
            dailyLifeSentence.sprite = currentCharacterSprite;

        }
        
        _characterUi.DisplayCharacters(dailyLifeSentence.character, dailyLifeSentence.sprite, dailyLifeSentence.protagSprite);
        
        previousCharacterSprite = currentCharacterSprite;
        previousProtagSprite = currentProtagSprite;
                
                
        // Cutscene images displayed here for now, if this gets complicated we may want a new class
        if (dailyLifeSentence.cutsceneImage != CutsceneImageID.None)
        {
            var newCutsceneImage = ResourceBank.Instance.GetSprite(dailyLifeSentence.cutsceneImage);
            _cutsceneImage.sprite = newCutsceneImage;
            _cutsceneImage.enabled = true;
        }
        else
        {
            _cutsceneImage.enabled = false;
        }

        if (dailyLifeSentence.characterEmote != EmoteID.None)
        {
            _emoteManager.CharacterEmote(dailyLifeSentence.characterEmote);
        }

        if (dailyLifeSentence.protagEmote != EmoteID.None)
        {
            _emoteManager.ProtagEmote(dailyLifeSentence.protagEmote);
        }

        if (dailyLifeSentence.isTalentScreen)
        {
            _talentScreen.gameObject.SetActive(true);
            _talentScreen.FadeIn();
            _talentScreen.StartAnimation(dailyLifeSentence.character);
        }

        if (dailyLifeSentence.cameraShake)
        {
            mainCamera.GetComponent<CameraMovement>().ShakeCamera();
        }

        if (dailyLifeSentence.characterPulse)
        {
            _characterUi.PulseCharacter();
        }

        if (dailyLifeSentence.protagPulse)
        {
            _characterUi.PulseProtag();
        }
    }

    public void EndDialogue(Action onCompletion, bool instant = false)
    {
        DialogueBox.EndDialogue(instant);
        _characterUi.MoveUiOut(onCompletion, instant);
        _cutsceneImage.enabled = false;

        previousCharacterSprite = CharacterSpriteID.None;
        previousProtagSprite = CharacterSpriteID.None;
        currentCharacterSprite = CharacterSpriteID.None;
        currentProtagSprite = CharacterSpriteID.None;
    }
    
    public bool CanAdvance()
    {
        return DialogueBox.IsIdle() && _characterUi.IsIdle();
    }

    private void UpdateSpriteChanges(DailyLifeSentence sentence)
    {
        currentCharacterSprite = sentence.sprite;
        _characterUi.CharacterSpriteHasChanged = (currentCharacterSprite != previousCharacterSprite); // See if character sprite has changed.
        
        currentProtagSprite = sentence.protagSprite;
        _characterUi.ProtagSpriteHasChanged = (currentProtagSprite != previousProtagSprite); // See if protag sprite has changed.
    }

    public void StartTruthBulletAnimation(TruthBulletID truthBulletId)
    {
        truthBulletVisible = true;
        _truthBulletAnimation.StartTruthBulletAnimation(truthBulletId);
    }

    public void FinishTruthBulletAnimation()
    {
        if (truthBulletVisible)
        {
            truthBulletVisible = false;
            _truthBulletAnimation.FinishTruthBulletAnimation();
        }
    }
    
    // Triggers a flashback.
    private void TriggerFlashback()
    {
        var graySettings = mainCamera.GetComponent<PostProcessingBehaviour>().profile.colorGrading.settings;
        graySettings.basic.saturation = 0; // 0 Saturation = grayscale.

        mainCamera.GetComponent<PostProcessingBehaviour>().profile.colorGrading.settings = graySettings; // Set gray camera.
        DialogueBox.TriggerFlashEffect();
        
        // Use the grayscale shader.
        _characterUi.currentPortrait.material = new Material(Shader.Find("GrayScale"));
        _characterUi.protagPortrait.material = new Material(Shader.Find("GrayScale"));

    }
    
    // Ends a flashback.
    private void EndFlashback()
    {
        var graySettings = mainCamera.GetComponent<PostProcessingBehaviour>().profile.colorGrading.settings;
        graySettings.basic.saturation = 1; // 1 saturation = colour.

        mainCamera.GetComponent<PostProcessingBehaviour>().profile.colorGrading.settings = graySettings; // Update the camera.
        DialogueBox.TriggerFlashEffect();
        
        // Revert to the default 'UI' shader.
        _characterUi.currentPortrait.material = new Material(Shader.Find("UI/Unlit/Transparent"));
        _characterUi.protagPortrait.material = new Material(Shader.Find("UI/Unlit/Transparent"));
    }

    public void FadeOutSideUI()
    {
        _characterUi.FadeOutUI();
    }

    public void FadeInSideUI()
    {
        _characterUi.FadeInUI();
    }

    public void PlayMonocoinGetAnimation()
    {
        _monocoinAnimation.PlayMonocoinGetAnimation();
    }

    public void AbortAllAnimations()
    {
        _characterUi.AbortAllAnimations();
        DialogueBox.AbortAllAnimations();
    }

}