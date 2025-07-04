using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance; // 싱글톤 패턴

    [SerializeField] private List<CardSO> cardList = new List<CardSO>(); // 모든 카드 데이터 저장

    private Dictionary<int, Card> cardDictionary = new Dictionary<int, Card>(); // 카드 ID 매칭용 Dictionary

    public List<CardSO> cardSOList;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(this);
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        foreach (var cardSO in cardList)
        {
            foreach (var card in cardSO.cards)
            {
                if (!cardDictionary.ContainsKey(card.cardId))
                {
                    cardDictionary.Add(card.cardId, card);
                }
                else
                {
                    Debug.LogWarning($"중복된 카드 ID 발견: {card.cardId}");
                }
            }
        }
    }

    public Card GetCardById(int cardId)
    {
        if (cardDictionary.ContainsKey(cardId))
        {
            return cardDictionary[cardId];
        }
        Debug.LogError($"카드 ID {cardId}를 찾을 수 없습니다.");
        return null;
    }

    public CardSO GetCardSOById(int cardId)
    {
        if (cardSOList == null || cardSOList.Count == 0)
            return null;

        foreach (var cardSO in cardSOList)
        {
            if (cardSO.cards.Exists(card => card.cardId == cardId))
            {
                return cardSO;
            }
        }

        return null;
    }
}
