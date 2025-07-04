using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseCardBuilder : MonoBehaviour
{
    [SerializeField]
    private Image cardImage;

    [SerializeField]
    private Text cardName, cardExplanation;
    

    private BaseCard _baseCard;

    private int _cardId;

    public void Init(CardSO cardSO, int cardId, BaseCard baseCard)
    {
        _cardId = cardId;

        Card targetCard = cardSO.cards.Find(card => card.cardId == cardId);

        if (targetCard != null)
        {
            cardImage.sprite = targetCard.artwork;
        }
        else
        {
            Debug.Log("해당 ID의 카드를 찾을 수 없습니다.");
        }
    }
}
