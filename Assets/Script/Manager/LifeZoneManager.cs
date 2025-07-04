using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;

public class LifeZoneManager : MonoBehaviourPun
{
    public static LifeZoneManager Instance;

    [Header("라이프 카드")]
    public Transform myLifePanel;                      //라이프 카드 UI 부모
    public Transform opponentLifePanel;
    public GameObject cardBackPrefab;                //뒷면 카드 프리팹

    private List<Card> myLifeCards = new();            //카드 데이터 저장
    private List<GameObject> myLifeObjects = new();
    private List<GameObject> opponentLifeObjects = new();

    [Header("선택 UI")]
    public GameObject lifeSelectionPanel;            // 선택 UI Panel
    public Transform selectionParent;                // 선택 카드 표시 위치

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))  // 테스트용 키
        {
            ShowLifeSelection();
        }
    }

    //라이프 카드 뽑고 뒷면 표시
    public void SetupInitialLife(List<Card> deck)
    {
        myLifeCards.Clear();
        myLifeObjects.Clear();

        for (int i = 0; i < 7; i++)
        {
            Card top = GameManager.Instance.DrawTopCardFromDeck();
            myLifeCards.Add(top);

            GameObject cardObj = Instantiate(cardBackPrefab, myLifePanel);
            myLifeObjects.Add(cardObj);
            cardObj.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 90);
        }
        ArrangeLifeUI(myLifePanel, myLifeObjects);
    }

    //라이프 패널 정렬
    private void ArrangeLifeUI(Transform panel, List<GameObject> cardObj)
    {
        photonView.RPC(nameof(RPC_UpdateOpponentLife), RpcTarget.Others, myLifeCards.Count);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        float cardRotatedHeight = 279f;  // 회전 후 높이 기준

        int count = cardObj.Count;
        float spacing = 0f;

        if (count > 1)
        {
            spacing = Mathf.Min((panelHeight - cardRotatedHeight) / (count - 1), cardRotatedHeight - 20f);
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = cardObj[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -i * spacing);
            rt.localRotation = Quaternion.Euler(0, 0, 90);
        }
    }

    [PunRPC]
    private void RPC_UpdateOpponentLife(int lifeCount)
    {
        foreach (var obj in opponentLifeObjects)
            Destroy(obj);
        opponentLifeObjects.Clear();

        for (int i = 0; i < lifeCount; i++)
        {
            GameObject card = Instantiate(cardBackPrefab, opponentLifePanel);
            card.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 90);
            opponentLifeObjects.Add(card);
        }

        ArrangeLifeUI(opponentLifePanel, opponentLifeObjects);
    }

    //공격 성공 시 상대 라이프 카드 선택 UI
    public void ShowLifeSelection()
    {
        lifeSelectionPanel.SetActive(true);

        // 기존 UI 제거
        foreach (Transform child in selectionParent)
            Destroy(child.gameObject);

        for (int i = 0; i < opponentLifeObjects.Count; i++)
        {
            int index = i;
            GameObject cardUI = Instantiate(cardBackPrefab, selectionParent);
            Button btn = cardUI.GetComponentInChildren<Button>();
            btn.onClick.AddListener(() =>
            {
                lifeSelectionPanel.SetActive(false);
                photonView.RPC(nameof(RPC_ResolveSelectLifeCard), RpcTarget.Others, index);
            });
        }
    }

    //선택된 라이프 카드 공개
    [PunRPC]
    public void RPC_ResolveSelectLifeCard(int index)
    {
        // 카드 데이터 가져오기
        Card cardData = myLifeCards[index];

        // GameBaseCard 생성 → 효과용
        CardSO cardSO = CardDatabase.Instance.GetCardSOById(cardData.cardId);
        GameBaseCard cardInstance = Instantiate(GameManager.Instance.CardPrefab);
        cardInstance.Init(cardSO, cardData.cardId);

        HandleTriggerEffect(cardData.cardTrigger, cardData, cardInstance);

        Destroy(myLifeObjects[index]);
        myLifeCards.RemoveAt(index);
        myLifeObjects.RemoveAt(index);

        if (myLifeCards.Count == 0)
        {
            GameManager.Instance.HandleGameOver();
        }
        photonView.RPC(nameof(RPC_UpdateOpponentLife), RpcTarget.Others, myLifeCards.Count);
    }

    //트리거 실행
    private void HandleTriggerEffect(EcardTrigger effect, Card card, GameBaseCard cardObj)
    {
        switch (effect)
        {
            case EcardTrigger.None:
                GraveyardManager.Instance.SendToGrave(card, true); 
                break;

            case EcardTrigger.Get:
                cardObj.transform.SetParent(MyHandManager.Instance.handArea, false);
                cardObj.transform.localPosition = Vector3.zero;
                MyHandManager.Instance.AddCardToHand(cardObj); 
                break;

            case EcardTrigger.Raid:
                {
                    var raidTargets = MyFieldManager.Instance.myFieldSlots
                                      .Where(slot => slot.HasCard() && slot.GetPlacedCard().cardData.cardName == card.cardName).ToList();

                    if (raidTargets.Count > 0)
                    {
                        TriggerUIManager.Instance.ShowYesNo("레이드 하시겠습니까?", onYes: () =>
                        {
                            TriggerUIManager.Instance.ShowCardSelection(raidTargets.Select(t => t.GetPlacedCard()).ToList(), selected =>
                                {
                                    selected.SetRaidSummoned(true); //레이드로 소환됨 표시
                                    selected.CurrentSlot.PlaceCard(cardObj, isStacked: true); // 위에 겹쳐 소환
                                    cardObj.SetActiveState();
                                });
                        },
                        onNo: () => {
                            cardObj.transform.SetParent(MyHandManager.Instance.handArea, false);
                            cardObj.transform.localPosition = Vector3.zero;
                            MyHandManager.Instance.AddCardToHand(cardObj);
                            });

                    }
                    else
                    {
                        cardObj.transform.SetParent(MyHandManager.Instance.handArea, false);
                        cardObj.transform.localPosition = Vector3.zero;
                        MyHandManager.Instance.AddCardToHand(cardObj);
                    }
                    break;
                }

            case EcardTrigger.Draw:
                GameManager.Instance.DrawCard();
                GraveyardManager.Instance.SendToGrave(card, true);
                break;

            case EcardTrigger.Final:
                if (myLifeCards.Count == 0 && GameManager.Instance.HasCardsInDeck())
                {
                    Card bonus = GameManager.Instance.DrawTopCardFromDeck();
                    myLifeCards.Add(bonus);

                    GameObject newCardObj = Instantiate(cardBackPrefab, myLifePanel);
                    myLifeObjects.Add(newCardObj);

                    ArrangeLifeUI(myLifePanel, myLifeObjects);
                }
                GraveyardManager.Instance.SendToGrave(card, true);
                break;

            case EcardTrigger.Active:
                {
                    var targets = MyFieldManager.Instance.myFieldSlots
                        .Where(slot => slot.IsMyFrontLine() && slot.HasCard()).ToList();

                    if (targets.Count > 0)
                    {
                        TriggerUIManager.Instance.ShowCardSelection(
                            targets.Select(s => s.GetPlacedCard()).ToList(),
                            selectedCard =>
                            {
                                selectedCard.ApplyTempBPBuff(3000);
                                GraveyardManager.Instance.SendToGrave(card, true);
                            }
                        );
                    }
                    else
                    {
                        GraveyardManager.Instance.SendToGrave(card, true);
                    }
                    break;
                }

            case EcardTrigger.Color:
                {
                    var targets = OpponentFieldManager.Instance.opponentFieldSlots
                        .Where(slot => slot.IsOpponentFrontLine() && slot.HasCard() && slot.GetPlacedCard().GetBP() <= 3500).ToList();

                    if (targets.Count > 0)
                    {
                        TriggerUIManager.Instance.ShowCardSelection(
                            targets.Select(s => s.GetPlacedCard()).ToList(),
                            selectedCard =>
                            {
                                GameManager.Instance.photonView.RPC(nameof(GameManager.RPC_AddCardToHand), RpcTarget.Others, selectedCard.cardId);
                                selectedCard.ClearFromField();

                                GraveyardManager.Instance.SendToGrave(card, true);
                            });
                    }
                    else
                    {
                        GraveyardManager.Instance.SendToGrave(card, true);
                    }
                    break;
                }

            case EcardTrigger.Special:
                {
                    var targets = OpponentFieldManager.Instance.opponentFieldSlots
                        .Where(slot => slot.IsOpponentFrontLine() && slot.HasCard()).ToList();

                    if (targets.Count > 0)
                    {
                        TriggerUIManager.Instance.ShowCardSelection(
                            targets.Select(s => s.GetPlacedCard()).ToList(),
                            selectedCard =>
                            {
                                GraveyardManager.Instance.SendToGrave(selectedCard.cardData, false);
                                selectedCard.ClearFromField(); // 필드에서 제거
                                GraveyardManager.Instance.SendToGrave(card, true); // 트리거 카드도 묘지로
                            });
                    }
                    else
                    {
                        GraveyardManager.Instance.SendToGrave(card, true);
                    }
                    break;
                }
        }
    }
}
