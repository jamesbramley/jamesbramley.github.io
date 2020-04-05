
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConceptSearchCardContainer : MonoBehaviour
{
    private List<Card> allCards;
    private GameObject container;

    private List<GameObject> displayedCards = new List<GameObject>();
    public TextMeshProUGUI searchBar;
    private string searchBarPreviousString = "";
    private GameObject conceptSearchCanvas;
    private bool firstFrame = true;

    private ScrollRect scrollRect;
    
    private void Start()
    {
        container = GameObject.Find("DBContentCSCC");
        conceptSearchCanvas = GameObject.Find("ConceptSearchCanvas");
        CardLoader.LoadCards();
        allCards = CardList.GetCardList().Values.ToList();
        Debug.Log(allCards.Count);
        foreach (var card in allCards)
        {
            InstantiateCard(card);
        }
        
        scrollRect = container.GetComponentInParent <ScrollRect>();
    }

    private void Update()
    {
        if (FilterChanged())
        {
            Filter(searchBar.text);
        }

        if (firstFrame)
        {
            firstFrame = false;
            conceptSearchCanvas.SetActive(false);
        }
    }

    private bool FilterChanged()
    {
        if (searchBarPreviousString.Equals(searchBar.text))
        {
            return false;
        }

        searchBarPreviousString = searchBar.text;
        return true;
    }
    
    private void InstantiateCard(Card card)
    {
        if (card.GetType() == typeof(BoiCard))
        {
            var cardItem = Instantiate(Resources.Load<GameObject>("prefabs/ConceptSearchCard"));
            cardItem.transform.SetParent(container.transform);
            cardItem.transform.localPosition = Vector3.zero;
            cardItem.transform.localScale = new Vector3(1, 1, 1);
            cardItem.GetComponent<ConceptSearchCard>().UpdateSlot(card);
            displayedCards.Add(cardItem);
        }
        
    }

    private void DestroyAllCards()
    {
        foreach (var displayedCard in displayedCards)
        {
            Destroy(displayedCard);
        }
    }

    private void Filter(string query)
    {
        query = query.Trim();
        DestroyAllCards();
        displayedCards.Clear();
        foreach (var card in allCards)
        {

            if (query.Equals(String.Empty))
            {
                InstantiateCard(card);
            }
            else if (card.Name.ToLower().Contains(query.ToLower()))
            {
                InstantiateCard(card);
            }
        }
        
        scrollRect.verticalNormalizedPosition = 1;

    }
    
    public void OpenConceptSearch()
    {
        PlayerStatus.acceptingInput = false;
        conceptSearchCanvas.SetActive(true);
    }

    public void CloseConceptSearch()
    {
        PlayerStatus.acceptingInput = true;
        conceptSearchCanvas.SetActive(false);
    }
    
}
