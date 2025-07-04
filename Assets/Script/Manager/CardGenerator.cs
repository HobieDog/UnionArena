using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardGenerator : MonoBehaviour
{
    private int generateNumber = 0;

    [SerializeField]
    private Transform _cardParent;
    [SerializeField]
    private BaseCard _baseCardPrefab;

    [SerializeField]
    private List<CardSO> cardSO;

    public void Awake()
    {
        foreach (CardSO cardData in cardSO)  // 🔥 List<CardSO>를 순회
        {
            foreach (Card card in cardData.cards)  // 🔥 개별 CardSO의 cards 리스트 접근
            {
                GenerateCard(generateNumber);
                generateNumber++;
            }
        }
    }

    public BaseCard GenerateCard(int id)
    {
        foreach (CardSO cardData in cardSO)
        {
            BaseCard baseCard = Instantiate(_baseCardPrefab, _cardParent);
            Card foundCard = cardData.cards.Find(card => card.cardId == id);
            baseCard.Init(cardData, id);

            return baseCard;
        }

        Debug.Log("해당 ID의 카드를 찾을 수 없습니다.");
        return null;
    }
}
