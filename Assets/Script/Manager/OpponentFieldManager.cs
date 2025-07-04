using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentFieldManager : MonoBehaviour
{
    public static OpponentFieldManager Instance { get; private set; }

    public List<FieldSlotUI> opponentFieldSlots;  // 필드 슬롯 목록
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
        foreach (var slot in opponentFieldSlots)
        {
            RectTransform rect = slot.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition))
            {
                return slot;
            }
        }
        return null;
    }

    public int GetSlotIndex(FieldSlotUI slot)
    {
        return opponentFieldSlots.IndexOf(slot);
    }
}
