using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

public class GameBaseCard : BaseCard, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPunObservable
{
    [SerializeField] private GameObject cardBack;  // 카드 뒷면
    [SerializeField] private Image cardOutline; // 카드 테두리

    private bool isFaceUp = true;  // 기본값: 내 카드 앞면
    private bool isDragging = false; // 드래그앤드랍 구현
    private bool isOnField = false;

    public PhotonView photonView;

    // 카드 설명 UI
    private CardInfoUI cardInfoUI;
    private bool IsInHand => transform.parent.GetComponent<MyHandManager>() != null;
    public FieldSlotUI CurrentSlot { get; private set; }
    public bool IsRaidSummoned { get; private set; } = false;
    public int OwnerActorNumber { get; private set; }
    private GameBaseCard stackedCard;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private int originalSortingOrder;
    private Canvas cardCanvas; // 카드의 Canvas 컴포넌트

    // BP UI
    [SerializeField] private TextMeshProUGUI bpText;
    [SerializeField] private Color defaultBPColor;
    [SerializeField] private Color buffedBPColor;

    private int baseBP;
    private int tempBP;

    //공격, 효과 버튼
    [SerializeField] private GameObject actionButtonPrefab;
    [SerializeField] private Transform buttonCanvasParent;
    private GameObject actionButtonInstance;

    //Active, Rest 구분
    public CardPositionState positionState = CardPositionState.Active;

    //카드 효과
    public List<ECardEffect> effects;
    private CardEffectHandler effectHandler;
    private int attackCount = 0;
    private int defenseCount = 0;

    public enum CardPositionState
    {
        Active,
        Rest
    }

    private Transform originalParent;
    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (photonView == null)
        {
            Debug.LogError("PhotonView가 프리팹에 추가되지 않았습니다!");
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 내가 데이터 전송
        {
            stream.SendNext(cardId);
            stream.SendNext(isOnField);
        }
        else // 다른 플레이어가 데이터 수신
        {
            cardId = (int)stream.ReceiveNext();
            isOnField = (bool)stream.ReceiveNext();
        }
    }

    private void Start()
    {
        cardInfoUI = FindObjectOfType<CardInfoUI>();  // 씬에서 CardInfoUI 자동 찾기
        cardCanvas = GetComponentInParent<Canvas>(); // Canvas 찾기

        originalScale = transform.localScale; // 원래 크기 저장

        if (cardCanvas != null)
        {
            originalSortingOrder = cardCanvas.sortingOrder; // 원래 정렬 순서 저장
        }
    }

    public new void Init(CardSO cardSO, int cardId)
    {
        base.Init(cardSO, cardId);  // 부모 클래스 Init() 호출

        if (_baseCardBuilder != null)
        {
            _baseCardBuilder.Init(cardSO, cardId, this);
        }

        baseBP = cardBattlePoint;
        tempBP = 0;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectButtonClick);

        //카드 효과 부여
        effectHandler = GetComponent<CardEffectHandler>();
        effectHandler.RegisterEffect(this);
        attackCount = 0;
        defenseCount = 0;

        //BP 표시
        UpdateVisualStats();
    }

    public void SetFaceUp(bool faceUp)
    {
        isFaceUp = faceUp;
        UpdateCardView();
    }

    private void UpdateCardView()
    {
        if (cardBack != null)
            cardBack.SetActive(!isFaceUp);
    }

    public void UpdateUsability(bool canUse)
    {
        if (cardOutline != null)
        {
            cardOutline.color = canUse ? Color.blue : Color.clear;
        }
    }

    public void CheckUsability()
    {
        bool canUse = MyHandManager.Instance.CanUseCard(cardData);  // `cardData` 사용
        UpdateUsability(canUse);
    }

    // 드래그 앤 드랍
    public void SetAsFieldCard()
    {
        isOnField = true;
        UpdateVisualStats();
    }

    //드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isOnField) 
            return;

        isDragging = true;
        originalParent = transform.parent;
        originalPosition = transform.position;

        transform.SetParent(GameManager.Instance.transform); // UI 계층에서 최상위로 이동 (다른 UI 요소 위로 보이게)
        cardCanvas.sortingOrder = 50; // 드래그 중에는 최상위로 배치
    }

    // 드래그 도즁
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            transform.position = eventData.position;

            //MyFieldManager.Instance.HighlightFieldSlot(eventData.position);
        }
    }

    // 드래그 끝
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // 페이즈 체크
        bool isStepPhase = TurnManager.Instance.isMyTurn && TurnManager.Instance.currentPhaseIndex == 1;
        bool isMainPhase = TurnManager.Instance.isMyTurn && TurnManager.Instance.currentPhaseIndex == 2;

        //레이드 카드 체크
        bool isRaid = false;
        bool isMove = (CurrentSlot != null);
        bool isFromHand = (CurrentSlot == null);

        if (cardData.cardType == ECardType.Raid)
        {
            FieldSlotUI ts = MyFieldManager.Instance.GetHoveredSlot(eventData.position);

            if (ts != null && ts.HasCard())
            {
                GameBaseCard existingCard = ts.GetPlacedCard();

                // 기존 카드가 Character 타입이고 이름이 동일한지 확인
                if (existingCard.cardData.cardType == ECardType.Character &&
                    existingCard.cardData.cardName == cardData.cardName)
                {
                    isRaid = true;
                }
            }
        }

        FieldSlotUI targetSlot = MyFieldManager.Instance.GetHoveredSlot(eventData.position);

        if (isStepPhase && CurrentSlot != null)
        {
            bool fromFront = CurrentSlot.IsMyFrontLine();
            bool toEnergy = targetSlot.IsEnergyLine();
            bool canMoveToEnergy = HasEffect(ECardEffect.Step);

            if (!(fromFront && toEnergy && canMoveToEnergy))
            {
                ReturnToOriginalPosition();
                return;
            }

            bool fromEnergy = CurrentSlot.IsEnergyLine();
            bool toFront = targetSlot.IsMyFrontLine();
            
            if (!(fromEnergy && toFront))
            {
                ReturnToOriginalPosition();
                return;
            }
        }

        if (targetSlot == null || (!targetSlot.CanPlaceCard() && !isRaid))
        {
            if (CurrentSlot == null)
                ReturnToHand();
            else
                ReturnToOriginalPosition();
            return;
        }

        FieldSlotUI previousSlot = CurrentSlot;

        if (targetSlot != null)
        {
            if (targetSlot.CanPlaceCard() || isRaid)
            {
                //메인 페이즈 및 자원 체크
                if (CurrentSlot == null)
                {
                    //스텝 페이즈의 경우 자동으로 메인 페이즈로 전환
                    if (isStepPhase)
                    {
                        TurnManager.Instance.ChangePhaseViaRPC(2); //메인 페이즈로 자동 전환
                        isMainPhase = true;
                    }

                    //메인 페이즈에서만 소환 가능
                    if (!isMainPhase)
                    {
                        ReturnToHand();
                        return;
                    }

                    //자원 체크
                    bool enough = GameManager.Instance.TryUseResources(cardData.cardUseActivePoint, cardData.cardUseCost);

                    if (!enough)
                    {
                        Debug.Log("자원이 부족하여 소환할 수 없습니다.");
                        ReturnToHand();
                        return;
                    }

                    //손패에서 제거
                    MyHandManager.Instance.RemoveCardFromHand(this);
                }
                else
                {
                    // 필드에서 다른 슬롯으로 이동 시도 Step 페이즈에만 허용
                    if (!isStepPhase)
                    {
                        Debug.Log("Step 페이즈에만 카드 이동이 가능합니다.");
                        ReturnToOriginalPosition();
                        return;
                    }
                }

                if (previousSlot != null && previousSlot != targetSlot)
                {
                    int prevSlotIndex = MyFieldManager.Instance.GetSlotIndex(previousSlot);
                    MyFieldManager.Instance.photonView.RPC("SyncClearSlot", RpcTarget.OthersBuffered, prevSlotIndex);
                }

                //필드 슬롯 배치
                SetCurrentSlot(targetSlot);
                targetSlot.PlaceCard(this, isRaid, isFromHand); // 필드에 배치   
                //if (targetSlot.IsEnergyLine())
                //{
                //    var emptyFrontSlot = MyFieldManager.Instance.myFieldSlots
                //        .Where(s => s.IsMyFrontLine() && !s.HasCard())
                //        .FirstOrDefault();

                //    if (emptyFrontSlot != null)
                //    {
                //        TriggerUIManager.Instance.ShowYesNo("FrontLine으로 이동하시겠습니까?",
                //        onYes: () =>
                //        {
                //            int slotIndex = MyFieldManager.Instance.GetSlotIndex(emptyFrontSlot);
                //            targetSlot.ClearSlot(false); // 현재 슬롯 비우고
                //            emptyFrontSlot.PlaceCard(this); // FrontLine으로 이동
                //        },
                //        onNo: () =>
                //        {

                //        });
                //    }
                //}

                //소환 구분
                if (isRaid || isMove)
                    SetActiveState();  //Riad = Active, 이동도 Active
                else
                    SetRestState();    //일반 소환 = Rest

                //소환 효과 발동
                if (isFromHand && isMainPhase)
                {
                    if (this.HasEffect(ECardEffect.DrawAndGrave1))
                        effectHandler.EnableDrawAndGraveEffect(1);
                    else if (this.HasEffect(ECardEffect.DrawAndGrave2))
                        effectHandler.EnableDrawAndGraveEffect(2);
                    else if (this.HasEffect(ECardEffect.BounceSummon))
                        effectHandler.HandleCostOneReturnEffect(this);
                    else if (this.HasEffect(ECardEffect.SummonCheckDrawChina))
                        effectHandler.CheckDeckTopAndSelect(4, card => card.cardBattlePoint <= 2500);
                }

                //배치 직후 코스트 재계산
                GameManager.Instance.RecalculateCost();
                transform.SetParent(targetSlot.transform, false);

                //포톤 동기화
                int slotIndex = MyFieldManager.Instance.GetSlotIndex(targetSlot);
                if (!PhotonNetwork.LocalPlayer.IsLocal)
                    return;

                bool isOpponentField = true;
                MyFieldManager.Instance.photonView.RPC("SyncPlacedCard", RpcTarget.AllBuffered, cardData.cardId, slotIndex, isOpponentField, isRaid, isMove);
            }
        }
    }

    private void ReturnToHand()
    {
        transform.SetParent(MyHandManager.Instance.transform, false);
        //MyHandManager.Instance.AddCardToHand(this); // 다시 핸드에 추가
        MyHandManager.Instance.ArrangeCards(); // 카드 정렬
        cardCanvas.sortingOrder = originalSortingOrder;
    }

    private void ReturnToOriginalPosition()
    {
        if (CurrentSlot != null)
        {
            transform.SetParent(CurrentSlot.transform, false);
            transform.localPosition = Vector3.zero;
        }
    }

    public void SetCurrentSlot(FieldSlotUI newSlot)
    {
        if (CurrentSlot == newSlot)
            return;

        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot(false);
        }

        CurrentSlot = newSlot;
    }

    public void ClearFromField()
    {
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isOnField)
        {
            cardInfoUI.ShowCardInfo(this);
            return;
        }

        originalPosition = transform.localPosition; // 원래 위치 저장
        if (cardCanvas != null)
        {
            cardCanvas.sortingOrder = 10; // 가장 앞에 오도록 정렬 변경
        }
        transform.localScale = originalScale * 1.2f; // 1.2배 확대
        transform.localPosition = originalPosition + new Vector3(0, 50f, 0); // 살짝 위로 이동

        cardInfoUI.ShowCardInfo(this);  // 카드 정보 표시
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isOnField)
        {
            cardInfoUI.HideCardInfo();
            return;
        }

        if (cardCanvas != null)
        {
            cardCanvas.sortingOrder = originalSortingOrder; // 원래 정렬 순서로 되돌리기
        }
        transform.localScale = originalScale; // 원래 크기로 되돌리기
        transform.localPosition = originalPosition; // 원래 위치로 되돌리기

        cardInfoUI.HideCardInfo();  // 카드 정보 숨김
    }

    public void OnSelectButtonClick()
    {
        if (actionButtonInstance != null)
        {
            Destroy(actionButtonInstance);
            return;
        }

        if (CurrentSlot == null || !IsActive())
            return;

        actionButtonInstance = Instantiate(actionButtonPrefab, buttonCanvasParent);
        actionButtonInstance.transform.localPosition = Vector3.zero;

        var tmpText = actionButtonInstance.GetComponentInChildren<TextMeshProUGUI>();
        tmpText.text = TurnManager.Instance.currentPhaseIndex == 3 ? "공격" : "효과";

        var button = actionButtonInstance.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (TurnManager.Instance.currentPhaseIndex == 3)
            {
                BattleManager.Instance.InitiateAttack(this);
            }
            else
            {
                // 카드 효과 발동 로직
            }
            Destroy(actionButtonInstance);
        });
    }

    //Raid 카드 처리
    public void ReturnRaidToHand(GameBaseCard raidCard)
    {
        GameBaseCard stacked = raidCard.GetStackedCard();

        if (stacked != null)
        {
            GraveyardManager.Instance.SendToGrave(stacked.cardData, true);
            Destroy(stacked.gameObject);
            SetStackedCard(null);
        }

        // 필드에서 제거되었기 때문에 슬롯에서도 분리
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot(false); // 단순히 슬롯 비움
            SetCurrentSlot(null);
        }
    }

    public int GetCost()
    {
        return cardData.cardMakeCost;
    }

    public int GetBP()
    {
        return cardData.cardBattlePoint;
    }

    public Sprite GetArtwork()
    {
        return cardData.artwork;
    }

    public void SetOwner(int actorNumber)
    {
        OwnerActorNumber = actorNumber;
    }

    // 레이드 소환 체크
    public void SetRaidSummoned(bool value)
    {
        IsRaidSummoned = value;
    }

    public void SetStackedCard(GameBaseCard underCard)
    {
        stackedCard = underCard;
    }

    public GameBaseCard GetStackedCard()
    {
        return stackedCard;
    }

    //버프 체크
    public void ApplyTempBPBuff(int amount)
    {
        tempBP += amount;
        UpdateVisualStats();

        if (CurrentSlot != null)
        {
            int slotIndex = MyFieldManager.Instance.GetSlotIndex(CurrentSlot);
            if (PhotonNetwork.LocalPlayer.IsLocal)
            {
                BattleManager.Instance.photonView.RPC("SyncTempBPBuff", RpcTarget.Others, slotIndex, tempBP);
            }
        }
    }

    public void ResetTempBP()
    {
        tempBP = 0;
        UpdateVisualStats();

        if (CurrentSlot != null)
        {
            int slotIndex = MyFieldManager.Instance.GetSlotIndex(CurrentSlot);
            if (PhotonNetwork.LocalPlayer.IsLocal)
            {
                BattleManager.Instance.photonView.RPC("SyncTempBPBuff", RpcTarget.Others, slotIndex, tempBP);
            }
        }
    }

    public int GetFinalBP()
    {
        return baseBP + tempBP;
    }

    public void UpdateVisualStats()
    {
        if (!isOnField)
        {
            bpText.text = ""; // 필드에 없으면 숨김
            return;
        }

        int finalBP = baseBP + tempBP;
        bpText.text = finalBP.ToString();

        if (tempBP > 0)
            bpText.color = buffedBPColor;
        else
            bpText.color = defaultBPColor;
    }

    public void SetTempBPDirectly(int value)
    {
        tempBP = value;
        UpdateVisualStats();
    }

    //Active, Rest
    public void SetActiveState()
    {
        positionState = CardPositionState.Active;
        transform.localRotation = Quaternion.Euler(0, 0, 0); // 세움
    }

    public void SetRestState()
    {
        positionState = CardPositionState.Rest;
        transform.localRotation = Quaternion.Euler(0, 0, 90); // 왼쪽으로 눕힘
    }

    public bool IsResting() => positionState == CardPositionState.Rest;
    public bool IsActive() => positionState == CardPositionState.Active;

    // 카드 효과
    public bool HasEffect(ECardEffect effect)
    {
        return effects.Contains(effect);
    }

    public void RegisterAttack()
    {
        attackCount++;

        // ExtraAttack 보유 카드만 2회 공격 허용
        if (!HasEffect(ECardEffect.ExtraAttack) || attackCount >= 2)
        {
            SetRestState();
        }
    }

    public void RegisterDefense()
    {
        defenseCount++;

        // ExtraAttack 보유 카드만 2회 공격 허용
        if (!HasEffect(ECardEffect.ExtraDefense) || defenseCount >= 2)
        {
            SetRestState();
        }
    }

    public void ResetCount()
    {
        attackCount = 0;
        defenseCount = 0;
    }
}
