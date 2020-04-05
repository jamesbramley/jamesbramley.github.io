
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public static class CardLoader
{
    private static string cardsDirectory = "Assets/cards_xml/cards.xml";
    private static string cardImagesDirectory = "CardArt/";

    private static Dictionary<int, Card> cards = new Dictionary<int, Card>();
    private static Dictionary<int, Sprite> cardSprites = new Dictionary<int, Sprite>();
    private static bool loaded = false;
    
    public static void LoadCards()
    {
        if (!loaded)
        {
            LoadXMLIntoCards();
        }
    }

    private static PowerCard powerCard = null;
    private static BoiCard boiCard = null;

    private static bool boiCreated;
    
    private static void LoadXMLIntoCards()
    {
        var xmlReader = XmlReader.Create(cardsDirectory);
        var idNumber = 0; // Variable for assigning unique ids.
        
        while (xmlReader.Read())
        {
            
            if (xmlReader.IsStartElement())
            {
                switch (xmlReader.Name)
                {
                    case "CardList":
                        break;
                    
                    case "Card":
                        if (xmlReader.GetAttribute("type") == "boi")
                        {
                            try
                            {
                                boiCard = new BoiCard();
                                boiCard.IdNumber = idNumber;
                                boiCard.Name = xmlReader.GetAttribute("name");
                                boiCard.MpCost = Int32.Parse(xmlReader.GetAttribute("mpCost"));
                                boiCard.Rarity = CardRarities.GetCardRarityID(xmlReader.GetAttribute("rarity"));
                                boiCard.atk = Int32.Parse(xmlReader.GetAttribute("atk"));
                                boiCard.def = Int32.Parse(xmlReader.GetAttribute("def"));
                                boiCard.originalATK = boiCard.atk;
                                boiCard.originalDEF = boiCard.def;
                                cardSprites.Add(idNumber, Resources.Load<Sprite>(cardImagesDirectory + xmlReader.GetAttribute("image"))); // Load in the image.
                                boiCard.Types = new List<string>();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Problem with XML initialising boi card: " + boiCard.Name + ". " + e);
                                throw;
                            }
                            
                            boiCreated = true;
                            
                        }
                        else
                        {
                            try
                            {
                                powerCard = new PowerCard();
                                powerCard.IdNumber = idNumber;
                                powerCard.Name = xmlReader.GetAttribute("name");
                                powerCard.MpCost = Int32.Parse(xmlReader.GetAttribute("mpCost"));
                                powerCard.Rarity = CardRarities.GetCardRarityID(xmlReader.GetAttribute("rarity"));
                                cardSprites.Add(idNumber, Resources.Load<Sprite>(cardImagesDirectory + xmlReader.GetAttribute("image"))); // Load in the image.

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Problem with XML initialising power card: " + powerCard.Name + ". " + e);
                                throw;
                            }
                            
                            boiCreated = false;

                        }

                        idNumber += 1;
                        break;
                    
                     case "AttackAbility":
                         boiCard.AttackAbility.Name = xmlReader.GetAttribute("name");
                         break;
                     case "PassiveAbility":
                         boiCard.PassiveAbility.Name = xmlReader.GetAttribute("name");
                         break;
                     
                     case "MagicAbility":
                         boiCard.MagicAbility.Name = xmlReader.GetAttribute("name");
                         try
                         {
                             boiCard.MagicAbility.MpCost = Int32.Parse(xmlReader.GetAttribute("mpCost"));
                             bool powerSurge;
                             boiCard.MagicAbility.PowerSurge = bool.TryParse(xmlReader.GetAttribute("powerSurge"), out powerSurge);
                         }
                         catch (Exception e)
                         {
                             Console.WriteLine("Problem with magic ability XML: " + boiCard.Name);
                             throw;
                         }
                         break;
                    case "Type":
                        boiCard.Types.Add(xmlReader.ReadInnerXml().Replace("&amp;", "&"));
                        break;
                     
                     case "AttackDescription":
                         boiCard.AttackAbility.Description = xmlReader.ReadInnerXml().Replace("&amp;", "&");
                         break;
                     
                     case "PassiveDescription":
                         boiCard.PassiveAbility.Description = xmlReader.ReadInnerXml().Replace("&amp;", "&");
                         break;
                     
                     case "MagicDescription":
                         boiCard.MagicAbility.Description = xmlReader.ReadInnerXml().Replace("&amp;", "&");
                         break;
                     
                     case "PowerAbility":
                         powerCard.PowerCardAbility.Name = xmlReader.GetAttribute("name");
                         bool continuous;
                         powerCard.Continuous = bool.TryParse(xmlReader.GetAttribute("continuous"), out continuous);
                         break;
                     
                     case "PowerDescription":
                         powerCard.PowerCardAbility.Description = xmlReader.ReadInnerXml().Replace("&amp;", "&");
                         break;
                     
                     
                     
                        
                }
            }
            else if (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                switch (xmlReader.Name)
                {
                    case "Card":
                        if (boiCreated)
                        {
                            cards.Add(boiCard.IdNumber, boiCard);
                        }
                        else
                        {
                            cards.Add(powerCard.IdNumber, powerCard);
                        }
                        break;
                }
            }
            
        }
        
        CardList.SetCardList(cards);
        CardList.SetCardSprites(cardSprites);
        loaded = true;
    }
    
}
