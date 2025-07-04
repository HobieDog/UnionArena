using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class MyHandManager : MonoBehaviourPun
{
    [SerializeField] public Transform handArea;  // 손패 배치할 부모 오브젝트
    [SerializeField] private GameBaseCard cardPrefab;
    public List<GameBaseCard> handCards = new List<GameBaseCard>();

    public RectTransform handAreaRect;

    // 소환 조건 달성 체크
    public int currentEnergy;
    public int currentActivePoint;
    public Dictionary<EcardColor, int> energyPool = new Dictionary<EcardColor, int>();

    public static MyHandManager Instance { get; private set; }

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
        // 색, 코스트 초기화
        foreach (EcardColor color in System.Enum.GetValues(typeof(EcardColor)))
        {
            energyPool[color] = 0;
        }
    }

    public bool CanUseCard(Card cardData)
    {
        if (cardData.cardUseCost == 0) 
            return true;

        bool hasEnoughEnergy = currentEnergy >= cardData.cardUseCost;
        bool hasCorrectColor = energyPool.ContainsKey(cardData.cardColor) && energyPool[cardData.cardColor] > 0;
        bool hasActivePoints = currentActivePoint >= cardData.cardUseActivePoint;

        return hasEnoughEnergy && hasCorrectColor && hasActivePoints;
    }

    public void AddCardToHand(GameBaseCard card)
    {
        handCards.Add(card);
        ArrangeCards();

        // 카드 사용 가능 체크
        card.CheckUsability();

        // 상대방에게 내 손패 개수 전송
        if (GameManager.Instance.OpponentHand != null)
        {
            GameManager.Instance.OpponentHand.photonView.RPC("SyncOpponentHandCount", RpcTarget.OthersBuffered, GetHandCount());
        }
    }

    public void RemoveCardFromHand(GameBaseCard card)
    {
        if (handCards.Contains(card))
        {
            handCards.Remove(card);
            ArrangeCards();
        }

        // 상대방에게 내 손패 개수 전송
        if (GameManager.Instance.OpponentHand != null)
        {
            GameManager.Instance.OpponentHand.photonView.RPC("SyncOpponentHandCount", RpcTarget.OthersBuffered, GetHandCount());
        }
    }

    public void ArrangeCards()
    {
        int count = handCards.Count;

        float radius = -3000f; // 곡률 반경 조정
        float angleStep = 2f; // 카드 간의 회전 각도
        float startAngle = -angleStep * (count - 1) / 2f;

        float cardSpacing = 140f;
        float handAreaWidth = handAreaRect.rect.width;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (angleStep * i);
            float radian = angle * Mathf.Deg2Rad;

            Vector3 position = new Vector3(Mathf.Sin(radian) * radius, -Mathf.Cos(radian) * radius, 0f);
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            float centerOffsetX = (-(cardSpacing * (count - 1)) / 2f) + (i * cardSpacing);

            handCards[i].transform.localPosition = position;
            handCards[i].transform.localRotation = rotation;
            handCards[i].transform.SetSiblingIndex(i); // 카드 순서 유지
        }
    }

    public List<GameBaseCard> GetAllCards()
    {
        return new List<GameBaseCard>(handCards); // 현재 손패 리스트 반환
    }

    public int GetHandCount()
    {
        return handCards.Count;
    }

    public void ClearHand()
    {
        handCards.Clear(); // 손패 리스트 비우기
    }
}
