using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public enum ECardEffect
{
    Impact,             // ����Ʈ
    DisImpact,          // ����Ʈ ��ȿ
    Step,               // ���� �̵�
    ExtraAttack,        // 2ȸ ����
    ExtraDefense,       // 2ȸ ���� 
    DrawAndGrave1,      // 1�� �̰� 1�� ����
    DrawAndGrave2,      // 2�� �̰� 2�� ����
    BounceSummon,       // �ʵ忡 ������ �´� ī�带 ���з� �ǵ����� ��ȯ
    SummonCheckDrawChina, //cardEffectMap[4] ġ�� �ɷ�
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
        // ī�� ID�� �׿� �����ϴ� ȿ������ ����
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

    // ī�� �̰� ������
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

        // ���� ���� ���纻 (ShowCardSelection ���ο��� ����ǹǷ�)
        var handCards = MyHandManager.Instance.handCards.ToList();

        bool selectionDone = false;

        TriggerUIManager.Instance.ShowCardSelection(handCards, selected =>
        {
            MyHandManager.Instance.RemoveCardFromHand(selected);
            GraveyardManager.Instance.SendToGrave(selected.cardData, true);
            Destroy(selected.gameObject);

            selectionDone = true;
        });

        //������ ������ ���
        yield return new WaitUntil(() => selectionDone);

        //���� ī�� ����
        StartCoroutine(GraveCardsCoroutine(count - 1));
    }

    // �ʵ忡 ������ �´� ī�带 ���з� �ǵ����� ��ȯ 
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

    // ���ϴ� �� ��ŭ �� ������ ī�� Ȯ���ϰ�, ������ ī�� �̰� ������
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
