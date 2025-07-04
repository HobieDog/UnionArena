using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OpponentHandManager : MonoBehaviourPun
{
    [SerializeField] private Transform handArea;  // 손패 배치할 부모 오브젝트
    [SerializeField] private GameObject cardBackPrefab; // 카드 뒷면 프리팹
    private List<GameObject> opponentCardBacks = new List<GameObject>();

    public void UpdateOpponentHand(int newCount)
    {
        while (opponentCardBacks.Count < newCount)
        {
            GameObject cardBack = Instantiate(cardBackPrefab, handArea);
            opponentCardBacks.Add(cardBack);
        }
        while (opponentCardBacks.Count > newCount)
        {
            Destroy(opponentCardBacks[0]);
            opponentCardBacks.RemoveAt(0);
        }

        ArrangeCards();
    }

    // 상대방에게 받은 손패 개수 적용
    [PunRPC]
    public void SyncOpponentHandCount(int handCount)
    {
        UpdateOpponentHand(handCount);
    }

    public void ArrangeCards()
    {
        float totalCards = opponentCardBacks.Count;
        float radius = 200f; // 곡률 반경 조정 (값을 조절하면서 테스트)
        float angleStep = 15f; // 카드 간 각도 조정
        float startAngle = -angleStep * (totalCards - 1) / 2; // 중앙 정렬

        for (int i = 0; i < opponentCardBacks.Count; i++)
        {
            float angle = startAngle + (angleStep * i);
            float radian = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환

            Vector3 cardPosition = new Vector3(Mathf.Sin(radian) * radius, -Mathf.Cos(radian) * radius, 0);
            Quaternion cardRotation = Quaternion.Euler(0, 0, angle);

            opponentCardBacks[i].transform.localPosition = cardPosition;
            opponentCardBacks[i].transform.localRotation = cardRotation;
        }
    }
}
