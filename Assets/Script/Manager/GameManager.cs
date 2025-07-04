using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPun
{
    public static GameManager Instance;
    public GameBaseCard CardPrefab => cardPrefab;

    //손패
    [SerializeField] private MyHandManager myHand;
    [SerializeField] private OpponentHandManager opponentHand;
    [SerializeField] private GameBaseCard cardPrefab;

    public List<Card> selectedDeck;
    private List<Card> currentHand = new List<Card>(); //현재 손패 저장

    public OpponentHandManager OpponentHand { get; private set; }

    //멀리건
    [SerializeField] private GameObject mulliganPanel; //멀리건 UI
    [SerializeField] private Button yesButton; //다시 뽑기 
    [SerializeField] private Button noButton;  //유지 

    //플레이어 체크
    private int playersReady = 0;

    //자원 관리
    public int myTurnCount = 0;
    public int turnCount = 1;
    public int currentAP = 0;
    public int maxAP = 0;
    public int cost = 0;
    public bool isFirstPlayer = false;

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

        OpponentHand = FindObjectOfType<OpponentHandManager>();
        myTurnCount = 0;
    }

    void Start()
    {
        LoadSelectedDeck();
        ShuffleAndDrawStartingHand();
    }

    public void LoadSelectedDeck()
    {
        DeckManager.Instance.LoadDecks();
        selectedDeck = DeckManager.Instance.GetSelectedDeck();
    }

    //덱에 남은 카드 체크
    public bool HasCardsInDeck()
    {
        return selectedDeck.Count > 0;
    }

    //게임 시작 드로우 및 멀리건UI 생성
    private void ShuffleAndDrawStartingHand()
    {
        selectedDeck = selectedDeck.OrderBy(c => UnityEngine.Random.value).ToList();
        currentHand.Clear();

        for (int i = 0; i < 7; i++)
        {
            DrawCard(selectedDeck[i]);
            currentHand.Add(selectedDeck[i]);
        }

        ShowMulliganUI();
    }

    //초반 멀리건용 드로우
    public void DrawCard(Card drawnCard)
    {
        selectedDeck.Remove(drawnCard);

        CardSO cardSO = CardDatabase.Instance.GetCardSOById(drawnCard.cardId);
        GameBaseCard newCard = Instantiate(cardPrefab, myHand.transform);
        newCard.Init(cardSO, drawnCard.cardId);

        myHand.AddCardToHand(newCard);
    }

    //게임 시작 후 드로우
    public void DrawCard()
    {
        Card drawnCard = DrawTopCardFromDeck();
        if (drawnCard == null) 
            return;

        currentHand.Add(drawnCard);

        CardSO cardSO = CardDatabase.Instance.GetCardSOById(drawnCard.cardId);
        GameBaseCard newCard = Instantiate(cardPrefab, myHand.transform);
        newCard.Init(cardSO, drawnCard.cardId);

        myHand.AddCardToHand(newCard);
    }

    public Card DrawTopCardFromDeck()
    {
        if (selectedDeck.Count == 0)
            return null;

        Card drawnCard = selectedDeck[0];
        selectedDeck.RemoveAt(0);
        return drawnCard;
    }

    //액스트라 드로우
    public void ExtraDraw()
    {
        if (currentAP >= 1)
        {
            currentAP--;
            DrawCard();  // 추가 드로우
        }
    }

    //멀리건UI
    private void ShowMulliganUI()
    {
        mulliganPanel.SetActive(true);
        yesButton.onClick.AddListener(DoMulligan);
        noButton.onClick.AddListener(ConfirmHand);
    }

    //멀리건
    private void DoMulligan()
    {
        //기존 손패를 덱으로 반환
        selectedDeck.AddRange(currentHand);
        selectedDeck = selectedDeck.OrderBy(c => UnityEngine.Random.value).ToList();

        //기존 카드 제거
        foreach (var card in myHand.GetAllCards())
        {
            Destroy(card.gameObject);
        }
        myHand.ClearHand();

        //새 손패 드로우
        List<Card> newHand = new List<Card>();
        for (int i = 0; i < 7; i++)
        {
            DrawCard(selectedDeck[i]);
            newHand.Add(selectedDeck[i]);
        }

        currentHand = newHand;

        ConfirmHand();
    }

    private void ConfirmHand()
    {
        mulliganPanel.SetActive(false);
        photonView.RPC("RPC_PlayerReady", RpcTarget.MasterClient);
        //라이프 활성화
        LifeZoneManager.Instance.SetupInitialLife(selectedDeck);
    }

    //모든 플레이어 멀리건 완료 확인
    [PunRPC]
    private void RPC_PlayerReady()
    {
        playersReady++;

        if (playersReady >= PhotonNetwork.CurrentRoom.PlayerCount && PhotonNetwork.IsMasterClient)
        {
            //선공 정하기
            int firstPlayerActorNumber = PhotonNetwork.PlayerList[Random.Range(0, PhotonNetwork.PlayerList.Length)].ActorNumber;
            photonView.RPC(nameof(RPC_StartFirstTurn), RpcTarget.All, firstPlayerActorNumber);
        }
    }

    //선공 시작
    [PunRPC]
    public void RPC_StartFirstTurn(int actorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            Debug.Log("내가 선공입니다");
            TurnManager.Instance.photonView.RPC("RPC_StartTurn", RpcTarget.All, turnCount, actorNumber, 0);
        }
        else
        {
            Debug.Log("상대가 선공입니다");
        }
    }

    //게임 턴 및 AP 관리
    public void OnTurnStart()
    {
        if (turnCount == 1)
            maxAP = 1;
        else if (turnCount > 1 && turnCount <= 4)
            maxAP = 2;
        else
            maxAP = 3;

        currentAP = maxAP;
        RecalculateCost();
    }

    //카드 낼 때 사용
    public bool TryUseResources(int useAP, int useCost)
    {
        if (currentAP >= useAP && cost >= useCost)
        {
            currentAP -= useAP;
            Debug.Log($"자원 사용 성공 - 남은 AP: {currentAP}");
            return true;
        }
        return false;
    }

    public void RecalculateCost()
    {
        cost = 0;

        foreach (var slot in MyFieldManager.Instance.myFieldSlots)
        {
            if (slot.IsEnergyLine())
            {
                if (slot.HasCard())
                {
                    GameBaseCard card = slot.GetPlacedCard();
                    if (card != null)
                    {
                        int producedCost = card.GetCost();
                        cost += producedCost;
                    }
                }
            }
        }
    }

    //트리거 color 상대 필드 상대 손패로 반환
    [PunRPC]
    public void RPC_AddCardToHand(int cardId)
    {
        CardSO cardSO = CardDatabase.Instance.GetCardSOById(cardId);
        GameBaseCard card = Instantiate(cardPrefab, myHand.transform).GetComponent<GameBaseCard>();
        card.Init(cardSO, cardId);

        card.transform.SetParent(MyHandManager.Instance.handArea, false);
        card.transform.localPosition = Vector3.zero;
        MyHandManager.Instance.AddCardToHand(card);

        card.ReturnRaidToHand(card);
    }

    public List<Card> PeekTopCardsFromDeck(int count)
    {
        return selectedDeck.Take(count).ToList();
    }

    public void AddCardToBottomOfDeck(Card card)
    {
        selectedDeck.Add(card);
    }

    //게임 끝
    public void HandleGameOver()
    {
        Debug.Log("게임 종료 - 패배 처리");
    }
}
