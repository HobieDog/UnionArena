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

    // ī�� ������ ������
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

    // ���� ���� ������Ʈ
    private void UpdateGraveyardSlot(Image slot, Card card, bool isPlayer)
    {
        slot.sprite = card.artwork; // ������ ī���� ��Ʈ��ũ�� �̹��� ��ü
        slot.color = Color.white;
        slot.preserveAspect = true;

        //���� ���� ����
        Button btn = slot.GetComponent<Button>();
        if (btn == null)
            btn = slot.gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OpenGraveyardView(isPlayer));
    }

    // ���� ���� UI ����
    public void OpenGraveyardView(bool isPlayer)
    {
        graveyardScrollView.SetActive(true);

        Transform content = graveyardScrollView.transform.Find("Viewport/Content");
        foreach (Transform child in content) 
            Destroy(child.gameObject);

        // ScrollView�� ī�� �߰�
        List<Card> graveyard = isPlayer ? playerGraveyard : opponentGraveyard;
        foreach (Card card in graveyard)
        {
            CardSO cardSO = CardDatabase.Instance.GetCardSOById(card.cardId);
            GameBaseCard newCard = Instantiate(cardPrefab, content).GetComponent<GameBaseCard>();
            newCard.Init(cardSO, card.cardId);
        }

        // �ݱ� ��ư ����
        Button closeButton = graveyardScrollView.transform.Find("CloseButton").GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => graveyardScrollView.SetActive(false));
    }
}
