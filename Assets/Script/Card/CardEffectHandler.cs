using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public enum ECardEffect
{
    Impact,             // 임팩트
    DisImpact,          // 임팩트 무효
    Step,               // 스텝 이동
    ExtraAttack,        // 2회 공격
    ExtraDefense,       // 2회 수비 
    DrawAndGrave1,      // 1장 뽑고 1장 묘지
    DrawAndGrave2,      // 2장 뽑고 2장 묘지
    BounceSummon,       // 필드에 조건이 맞는 카드를 손패로 되돌리고 소환
    SummonCheckDrawChina, //cardEffectMap[4] 치나 능력
}

public class CardEffectHandler : MonoBehaviour
{
    private Dictionary<int, List<ECardEffect>> cardEffectMap = new();

    private void Awake()
    {
        InitEffectMap();
    }

    private void InitEffectMap()
    {
        // 카드 ID와 그에 대응하는 효과들을 정의
        cardEffectMap[0] = new List<ECardEffect> { ECardEffect.BounceSummon };
        cardEffectMap[1] = new List<ECardEffect> { ECardEffect.DisImpact };
        cardEffectMap[3] = new List<ECardEffect> { ECardEffect.DisImpact };
        cardEffectMap[4] = new List<ECardEffect> { ECardEffect.SummonCheckDrawChina };
    }

    public List<ECardEffect> GetEffects(int cardId)
    {
        return cardEffectMap.ContainsKey(cardId) ? cardEffectMap[cardId] : new();
    }

    public void RegisterEffect(GameBaseCard card)
    {
        List<ECardEffect> effects = GetEffects(card.cardData.cardId);
        card.effects = effects;

        foreach (var effect in effects)
        {
            switch (effect)
            {
                case ECardEffect.Impact:
                    break;

                case ECardEffect.DisImpact:
                    break;

                case ECardEffect.Step:
                    break;

                case ECardEffect.ExtraAttack:
                    break;

                case ECardEffect.ExtraDefense:
                    break;

                case ECardEffect.DrawAndGrave1:
                    break;

                case ECardEffect.DrawAndGrave2:
                    break;

                case ECardEffect.BounceSummon:
                    break;
            }
        }
    }

    // 카드 뽑고 버리기
    public void EnableDrawAndGraveEffect(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameManager.Instance.DrawCard();
        }

        StartCoroutine(GraveCardsCoroutine(count));
    }

    private IEnumerator GraveCardsCoroutine(int count)
    {
        if (count <= 0)
            yield break;

        yield return null;

        // 현재 손패 복사본 (ShowCardSelection 내부에서 변경되므로)
        var handCards = MyHandManager.Instance.handCards.ToList();

        bool selectionDone = false;

        TriggerUIManager.Instance.ShowCardSelection(handCards, selected =>
        {
            MyHandManager.Instance.RemoveCardFromHand(selected);
            GraveyardManager.Instance.SendToGrave(selected.cardData, true);
            Destroy(selected.gameObject);

            selectionDone = true;
        });

        //선택할 때까지 대기
        yield return new WaitUntil(() => selectionDone);

        //다음 카드 선택
        StartCoroutine(GraveCardsCoroutine(count - 1));
    }

    // 필드에 조건이 맞는 카드를 손패로 되돌리고 소환 
    public void HandleCostOneReturnEffect(GameBaseCard selfCard)
    {
        var lowCostCards = MyFieldManager.Instance.myFieldSlots
                                .Where(slot => slot.HasCard())
                                .Select(slot => slot.GetPlacedCard())
                                .Where(card => card.cardData.cardUseCost <= 1 && card != selfCard)
                                .ToList();

        if (lowCostCards.Count > 0)
        {
            TriggerUIManager.Instance.ShowCardSelection(lowCostCards, selected =>
            {
                selected.CurrentSlot.ClearSlot(false);
                MyHandManager.Instance.AddCardToHand(selected);
            });
        }
        else
        {
            selfCard.CurrentSlot.ClearSlot(false);
            MyHandManager.Instance.AddCardToHand(selfCard);
        }
    }

    // 원하는 수 만큼 덱 위에서 카드 확인하고, 선택한 카드 뽑고 버리기
    public void CheckDeckTopAndSelect(int count, Func<Card, bool> condition)
    {
        List<Card> topCards = GameManager.Instance.PeekTopCardsFromDeck(count);
        if (topCards == null || topCards.Count == 0)
            return;

        var validCards = topCards.Where(condition).ToList();
        if (validCards.Count == 0)
        {
            foreach (var card in topCards)
                GameManager.Instance.AddCardToBottomOfDeck(card);
            return;
        }

        List<GameBaseCard> validCardInstances = new();
        foreach (var card in validCards)
        {
            CardSO cardSO = CardDatabase.Instance.GetCardSOById(card.cardId);
            GameBaseCard cardInstance = Instantiate(GameManager.Instance.CardPrefab);
            cardInstance.Init(cardSO, card.cardId);
            validCardInstances.Add(cardInstance);
        }

        TriggerUIManager.Instance.ShowCardSelection(validCardInstances, selectedCard =>
        {
            MyHandManager.Instance.AddCardToHand(selectedCard);

            foreach (var instance in validCardInstances)
            {
                if (instance.cardData.cardId != selectedCard.cardId)
                {
                    GameManager.Instance.AddCardToBottomOfDeck(instance.cardData);
                    instance.gameObject.SetActive(false);
                    Destroy(instance.gameObject);
                } 
            }

            StartCoroutine(GraveCardsCoroutine(1));
        }); 
    }
}
