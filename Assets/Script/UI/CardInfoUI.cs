using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfoUI : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text cardName;
    [SerializeField] private TMP_Text cardType;
    [SerializeField] private TMP_Text cardExplanation;
    [SerializeField] private TMP_Text cardUseCost;
    [SerializeField] private TMP_Text cardUseActinPoint;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>(); // 자동 할당
        canvasGroup.alpha = 0;
    }

    public void ShowCardInfo(GameBaseCard gamebasecard)
    {
        Card targetCard = gamebasecard.CardSO.cards.Find(card => card.cardId == gamebasecard.cardId);

        if (targetCard != null)
        {
            cardImage.sprite = targetCard.artwork;  // 카드 이미지 가져오기
            cardName.text = targetCard.cardName;
            cardType.text = targetCard.cardType.ToString();
            cardExplanation.text = targetCard.cardExplanation; // 효과 설명 가져오기
            cardUseCost.text = $"Cost : " + targetCard.cardUseCost.ToString();
            cardUseActinPoint.text = $"AP : " + targetCard.cardUseActivePoint.ToString();
        }
        canvasGroup.alpha = 1;
    }

    public void HideCardInfo()
    {
        canvasGroup.alpha = 0;
    }
}
