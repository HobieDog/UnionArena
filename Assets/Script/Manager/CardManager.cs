using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [SerializeField] 
    private CardSO cardSO;
    private Dictionary<int, Card> cardDictionary = new Dictionary<int, Card>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCardDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCardDictionary()
    {
        foreach (Card card in cardSO.cards)
        {
            if (!cardDictionary.ContainsKey(card.cardId))
            {
                cardDictionary.Add(card.cardId, card);
            }
        }
    }

    public Card GetCardById(int cardId)
    {
        if (cardDictionary.TryGetValue(cardId, out Card card))
        {
            return card;
        }
        return null;
    }
}
