using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class FieldSlotUI : MonoBehaviourPun
{
    private GameBaseCard placedCard;
    [SerializeField] private Image outline;

    public void SetOutline(bool active, bool isOpponentField)
    {
        if (isOpponentField)
        {
            outline.enabled = false;
        }
        else
        {
            outline.enabled = true;
            outline.color = active ? Color.yellow : Color.white;
        }
    }

    public void PlaceCard(GameBaseCard card, bool isStacked = false, bool isFromHand = false)
    {
        if (!isStacked && !CanPlaceCard()) 
            return;

        if (isStacked && placedCard != null)
        {
            card.SetStackedCard(placedCard);
            placedCard.gameObject.SetActive(false);
        }

        placedCard = card;
        card.SetCurrentSlot(this);

        card.transform.SetParent(transform, false);
        card.transform.localPosition = Vector3.zero;

        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = slotRect.sizeDelta;

        Image[] images = card.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            RectTransform imageRect = img.GetComponent<RectTransform>();
            imageRect.sizeDelta = slotRect.sizeDelta;
        }

        card.transform.localRotation = Quaternion.Euler(0, 0, 0);
        card.SetAsFieldCard();

        MyFieldManager.Instance.AddCardToField(card); // 필드 카드 리스트에 추가
    }

    public void ClearSlot(bool isOpponent)
    {
        if (placedCard != null)
        {
            GameBaseCard tempCard = placedCard;
            placedCard = null;

            if (isOpponent)
            {
                if (tempCard != null)
                {
                    Destroy(tempCard.gameObject);
                    tempCard.SetCurrentSlot(null);
                }
            }
            else
            {
                tempCard.SetCurrentSlot(null);
            }
        }
    }

    public bool CanPlaceCard()
    {
        return placedCard == null;
    }

    public GameBaseCard GetPlacedCard()
    {
        return placedCard;
    }

    public bool HasCard()
    {
        return placedCard != null;
    }

    //에너지 라인 구분
    public bool IsEnergyLine()
    {
        int index = MyFieldManager.Instance.GetSlotIndex(this);
        return index >= 0 && index <= 3;
    }

    public bool IsMyFrontLine()
    {
        int index = MyFieldManager.Instance.GetSlotIndex(this);
        return index >= 4 && index <= 7;
    }

    public bool IsOpponentFrontLine()
    {
        int index = OpponentFieldManager.Instance.GetSlotIndex(this);
        return index >= 4 && index <= 7;
    }
}
