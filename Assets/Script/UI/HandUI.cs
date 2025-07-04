using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HandUI : MonoBehaviour
{
    //[SerializeField] private BaseCard baseCard;  // 앞면 이미지
    [SerializeField] private GameObject cardBack;  // 뒷면 오브젝트 (카드 뒷면)
    [SerializeField] private GameBaseCard gamebaseCard;

    public void Init(Card cardData, bool isMyCard)
    {
        if (gamebaseCard != null) // 이미 BaseCard가 있으면 생성하지 않음
            return;

        gamebaseCard.SetCardData(cardData);
        gamebaseCard.SetFaceUp(isMyCard);
    }

    public void AddCardToHand(GameBaseCard card)
    {
        card.transform.SetParent(transform, false);
    }
}
