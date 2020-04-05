
using System;
using System.Collections;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckEditCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image cardImage;
    public Card card;
    private Image backgroundImage;

    private Rect cardRect;

    public bool Selected { get; set; }

    private float followSpeed = 0.5f;

    private Camera cam;

    private CardInfo cardInfo; // The card info display.
    private DeckEditInfo deckEditInfo;

    public bool InDeck { get; set; }
    private DeckEditDeckContainer deckContainer;
    private DeckEditAllCardsContainer allCardsContainer;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        cardInfo = GameObject.Find("CardInfo").GetComponent<CardInfo>();
        deckEditInfo = GameObject.Find("DeckEditInfo").GetComponent<DeckEditInfo>();
        deckContainer = GameObject.Find("DeckContent").GetComponent<DeckEditDeckContainer>();
        allCardsContainer = GameObject.Find("DBContent").GetComponent<DeckEditAllCardsContainer>();
    }

    private void Update()
    {
        
        if (Selected)
        {
            //Debug.Log(card.Name + " Selected");
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (InDeck)
                {
                    RemoveFromDeck();
                }
                else
                {
                    AddToDeck();
                }
            }
            
        }
        
    }
    
    public void Remove()
    {
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Selected = true;
        backgroundImage.color = Color.gray;
        cardInfo.UpdateInfo(card);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
        Selected = false; // Prevent setting selected to false if it is picked up.
        
        backgroundImage.color = Color.white;
        cardInfo.UpdateInfo(card, true);
    }

    private void RemoveFromDeck()
    {
        deckEditInfo.RemoveCardFromDeck(card);
        Remove();
    }

    private void AddToDeck()
    {
        deckEditInfo.AddCardToDeck(card);
        deckContainer.AddCard(card);
    }

    private void UpdateImage(Sprite sprite)
    {
        cardImage.sprite = sprite;
    }

    public void UpdateSlot(Card card)
    {
        this.card = card;
        UpdateImage(CardList.GetCardSprite(card.IdNumber));
    }

    
}
