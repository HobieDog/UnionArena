using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GraveyardManager : MonoBehaviourPun
{
    public static GraveyardManager Instance;

    [SerializeField] private Image myGraveyardSlot;
    [SerializeField] private Image opponentGraveyardSlot;

    [SerializeField] private GameObject graveyardScrollView;
    [SerializeField] private GameBaseCard cardPrefab;

    public GameBaseCard CardPrefab => cardPrefab;

    private List<Card> playerGraveyard = new List<Card>();
    private List<Card> opponentGraveyard = new List<Card>();

    private PhotonView pv;

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
        pv = GetComponent<PhotonView>();
    }

    // 카드 묘지로 보내기
    public void SendToGrave(Card card, bool isPlayer)
    {
        if (isPlayer)
        {
            playerGraveyard.Add(card);
            UpdateGraveyardSlot(myGraveyardSlot, card, true);

            photonView.RPC(nameof(RPC_SyncOpponentGraveyard), RpcTarget.Others, card.cardId);
        }
        else
        {
            opponentGraveyard.Add(card);
            UpdateGraveyardSlot(opponentGraveyardSlot, card, false);

            photonView.RPC(nameof(RPC_SyncMyGraveyard), RpcTarget.Others, card.cardId);
        }
    }

    [PunRPC]
    public void RPC_SyncOpponentGraveyard(int cardId)
    {
        CardSO cardSO = CardDatabase.Instance.GetCardSOById(cardId);
        Card targetCard = cardSO.cards.Find(c => c.cardId == cardId);

        Card syncedCard = new Card
        {
            cardId = cardId,
            artwork = targetCard.artwork
        };

        opponentGraveyard.Add(syncedCard);
        UpdateGraveyardSlot(opponentGraveyardSlot, syncedCard, false);
    }

    [PunRPC]
    public void RPC_SyncMyGraveyard(int cardId)
    {
        CardSO cardSO = CardDatabase.Instance.GetCardSOById(cardId);
        Card targetCard = cardSO.cards.Find(c => c.cardId == cardId);

        Card syncedCard = new Card
        {
            cardId = cardId,
            artwork = targetCard.artwork
        };

        opponentGraveyard.Add(syncedCard);
        UpdateGraveyardSlot(myGraveyardSlot, syncedCard, false);
    }

    // 묘지 슬롯 업데이트
    private void UpdateGraveyardSlot(Image slot, Card card, bool isPlayer)
    {
        slot.sprite = card.artwork; // 마지막 카드의 아트워크로 이미지 교체
        slot.color = Color.white;
        slot.preserveAspect = true;

        //묘지 보기 오픈
        Button btn = slot.GetComponent<Button>();
        if (btn == null)
            btn = slot.gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OpenGraveyardView(isPlayer));
    }

    // 묘지 보기 UI 열기
    public void OpenGraveyardView(bool isPlayer)
    {
        graveyardScrollView.SetActive(true);

        Transform content = graveyardScrollView.transform.Find("Viewport/Content");
        foreach (Transform child in content) 
            Destroy(child.gameObject);

        // ScrollView에 카드 추가
        List<Card> graveyard = isPlayer ? playerGraveyard : opponentGraveyard;
        foreach (Card card in graveyard)
        {
            CardSO cardSO = CardDatabase.Instance.GetCardSOById(card.cardId);
            GameBaseCard newCard = Instantiate(cardPrefab, content).GetComponent<GameBaseCard>();
            newCard.Init(cardSO, card.cardId);
        }

        // 닫기 버튼 설정
        Button closeButton = graveyardScrollView.transform.Find("CloseButton").GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => graveyardScrollView.SetActive(false));
    }
}
