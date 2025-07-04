using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckCardUI : MonoBehaviour
{
    [SerializeField] private Text cardNameText; // 카드 이름 텍스트
    [SerializeField] private Text cardCountText; // 카드 개수 텍스트
    [SerializeField] private Button addButton; // + 버튼
    [SerializeField] private Button removeButton; // - 버튼

    private int cardCount = 1; // 기본 카드 개수
    private Card cardData; // 현재 카드 데이터

    public void Init(Card card)
    {
        cardData = card;
        cardNameText.text = card.cardRare + " " + card.cardName;
        cardCountText.text = cardCount.ToString();

        addButton.onClick.AddListener(() => IncreaseCardCount());
        removeButton.onClick.AddListener(() => DecreaseCardCount());
    }

    public void IncreaseCardCount()
    {
        if (DeckManager.Instance.GetTotalDeckSize() >= DeckManager.MAX_DECK_SIZE)
        {
            return; // 덱 개수가 50장이면 추가 불가
        }

        if (cardCount >= 4) // 최대 4장 제한
        {
            return;
        }

         cardCount++;
         UpdateUI();
         DeckManager.Instance.UpdateCardCount(cardData.cardId, cardCount); // 덱 데이터 업데이트
    }

    public void DecreaseCardCount()
    {
        cardCount = Mathf.Max(0, cardCount - 1);
        UpdateUI();
        if (cardCount <= 0)
        {
            DeckManager.Instance.RemoveCardFromDeck(cardData);
            Destroy(gameObject);
        }
        else
        {
            DeckManager.Instance.UpdateCardCount(cardData.cardId, cardCount); 
        }
    }

    //DeckManger의 LoadDeck()에서 카드 수를 설정
    public void SetCardCount(int count)
    {
        cardCount = Mathf.Clamp(count, 1, 4);
        UpdateUI();
    }

    private void UpdateUI()
    {
        cardCountText.text = cardCount.ToString();
    }

    public int GetCardCount()
    {
        return cardCount;
    }
}
