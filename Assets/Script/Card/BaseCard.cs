using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public enum EPhaseCheck
{
    DrawPhase,
    StepPhase,
    MainPhase,
    AttackPhase,
    EndPhase,
}

public enum EusingEffect
{
    InHand,
    Summon,
    UnSummon,
    MyTurn,
    Attack,
    Defense,
    Using,
}
public class BaseCard : MonoBehaviour
{
    public int cardId;
    public ECardType cardType;
    public string cardName;

    public int cardUseCost;
    public int cardMakeCost;
    public int cardUseActivePoint;
    public int cardBattlePoint;
    public EcardColor cardColor;

    public Button cardButton;

    [SerializeField]
    private CardController _cardController;
    [SerializeField]
    protected BaseCardBuilder _baseCardBuilder;
    [SerializeField]
    public Button selectButton;

    private CardSO _cardSO;

    public Card cardData;
    public CardSO CardSO => _cardSO;
    public CardController cardController => _cardController;

    public void Init(CardSO cardSO, int cardId)
    {
        if (_cardSO != null)
            return;

        _cardSO = cardSO;
        Card foundCard = cardSO.cards.Find(card => card.cardId == cardId);

        if (foundCard != null)
        {
            cardData = foundCard;  // 카드 데이터 저장
            _baseCardBuilder.Init(cardSO, cardId, this);

            this.cardId = cardId;
            cardType = foundCard.cardType;
            cardName = foundCard.cardName;

            cardUseCost = foundCard.cardUseCost;
            cardMakeCost = foundCard.cardMakeCost;
            cardUseActivePoint = foundCard.cardUseActivePoint;
            cardBattlePoint = foundCard.cardBattlePoint;
            cardColor = foundCard.cardColor;
        }
        else
        {
            Debug.LogError($"카드 ID {cardId}를 찾을 수 없습니다");
        }

        selectButton.onClick.RemoveAllListeners(); // 기존 이벤트 제거
        selectButton.onClick.AddListener(() => DeckManager.Instance.AddCardToDeck(cardData));
    }

    public void SetCardData(Card data)
    {
        if (data == null)
            return;

        cardData = data; // 카드 데이터 저장
        cardId = data.cardId;
        cardType = data.cardType;
        cardName = data.cardName;

        _baseCardBuilder.Init(_cardSO, cardId, this); // 카드 UI 업데이트
    }
}
