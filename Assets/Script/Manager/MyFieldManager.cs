using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MyFieldManager : MonoBehaviourPun
{
    public static MyFieldManager Instance { get; private set; }

    public List<FieldSlotUI> myFieldSlots;  // 필드 슬롯 목록
    private List<GameBaseCard> placedCards = new List<GameBaseCard>();

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
    }

    public FieldSlotUI GetHoveredSlot(Vector2 pointerPosition)
    {
        foreach (var slot in myFieldSlots)
        {
            RectTransform rect = slot.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition))
            {
                return slot;
            }
        }
        return null;
    }

    public void HighlightFieldSlot(Vector2 pointerPosition)
    {
        foreach (var slot in myFieldSlots)
        {
            if (slot.HasCard())
                continue;

            slot.SetOutline(false, false);
        }

        FieldSlotUI hoveredSlot = GetHoveredSlot(pointerPosition);
        if (hoveredSlot != null)
        {
            hoveredSlot.SetOutline(true, false);
        }
    }

    public void AddCardToField(GameBaseCard card)
    {
        placedCards.Add(card);
    }

    public void RemoveCardFromField(GameBaseCard card)
    {
        placedCards.Remove(card);
    }

    public List<GameBaseCard> GetPlacedCards()
    {
        return new List<GameBaseCard>(placedCards);
    }

    public int GetSlotIndex(FieldSlotUI slot)
    {
        return myFieldSlots.IndexOf(slot);
    }

    [PunRPC]
    public void SyncPlacedCard(int cardId, int slotIndex, bool isOpponent, bool isStacked, bool isMoved, PhotonMessageInfo info)
    {
        bool isSenderOpponent = (info.Sender != PhotonNetwork.LocalPlayer);
        List<FieldSlotUI> targetSlots = isSenderOpponent ? OpponentFieldManager.Instance.opponentFieldSlots : MyFieldManager.Instance.myFieldSlots;

        if (!isSenderOpponent) // 내가 소환한 카드는 생성하지 않음
            return;

        if (targetSlots == null || targetSlots.Count <= slotIndex)
            return;

        FieldSlotUI targetSlot = targetSlots[slotIndex];

        Card cardData = CardDatabase.Instance.GetCardById(cardId);
        CardSO cardSO = CardDatabase.Instance.GetCardSOById(cardId);

        if (cardData == null || cardSO == null)
            return;

        GameBaseCard newCard = Instantiate(GameManager.Instance.CardPrefab);
        newCard.Init(cardSO, cardData.cardId);
        newCard.SetCurrentSlot(targetSlot);

        if (isStacked && targetSlot.HasCard())
        {
            GameBaseCard baseCard = targetSlot.GetPlacedCard();
            newCard.SetStackedCard(baseCard);
            baseCard.gameObject.SetActive(false);
            targetSlot.PlaceCard(newCard, true);
            newCard.SetActiveState(); // Raid로 겹쳤으니 Active 상태
        }
        else
        {
            targetSlot.PlaceCard(newCard, false);
            if (isMoved)
                newCard.SetActiveState();
            else
                newCard.SetRestState(); // 일반 소환은 Rest
        }
    }

    [PunRPC]
    public void SyncClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= OpponentFieldManager.Instance.opponentFieldSlots.Count)
            return;

        FieldSlotUI targetSlot = OpponentFieldManager.Instance.opponentFieldSlots[slotIndex];

        if (targetSlot != null && targetSlot.HasCard()) //카드가 있는 경우만 삭제
        {
            targetSlot.ClearSlot(true);
        }
    }
}
