using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TurnManager : MonoBehaviourPun
{
    public static TurnManager Instance { get; private set; }

    public bool isMyTurn { get; private set; } = false;
    public int currentPhaseIndex { get; private set; } = -1;

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

    public void StartMyTurn()
    {
        isMyTurn = true;
        currentPhaseIndex = 0; // 드로우 페이즈 자동 실행
        ResetAllTempBuffs();

        GameManager.Instance.OnTurnStart();
        StartCoroutine(ExecuteDrawPhase());
    }

    private void StartNextTurn(int currentActor)
    {
        GameManager.Instance.turnCount++;

        int nextTurn = GameManager.Instance.turnCount;
        int nextPlayerActor = GetNextActorNumber(currentActor);

        photonView.RPC(nameof(RPC_StartTurn), RpcTarget.All, nextTurn, nextPlayerActor, 0);
    }

    [PunRPC]
    public void RPC_StartTurn(int newTurnCount, int actorNumber, int phaseIndex)
    {
        GameManager.Instance.turnCount = newTurnCount;

        //공통 UI 업데이트
        string phaseName = GetPhaseName(phaseIndex);
        TurnUI.Instance.UpdateTurnInfo(newTurnCount, phaseName);

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            StartMyTurn(); // 내가 턴 주인이라면 드로우부터 시작
        }
        else
        {
            isMyTurn = false;
            currentPhaseIndex = phaseIndex;
        }
    }

    public void EndMyTurn()
    {
        isMyTurn = false;
        currentPhaseIndex = -1;

        //모든 카드 액티브 상태로 만들기
        photonView.RPC(nameof(RPC_ActivateAllCards), RpcTarget.All);

        int currentActor = PhotonNetwork.LocalPlayer.ActorNumber;

        //마스터 클라이언트가 다음 턴을 시작하게 함
        if (PhotonNetwork.IsMasterClient)
        {
            StartNextTurn(currentActor);
        }
        else
        {
            photonView.RPC(nameof(RequestNextTurn), RpcTarget.MasterClient, currentActor);
        }
    }

    [PunRPC]
    public void RPC_ActivateAllCards()
    {
        foreach (var slot in MyFieldManager.Instance.myFieldSlots)
        {
            if (slot.HasCard())
            {
                slot.GetPlacedCard().SetActiveState();

            }
               
        }

        foreach (var slot in OpponentFieldManager.Instance.opponentFieldSlots)
        {
            if (slot.HasCard())
            {
                slot.GetPlacedCard().SetActiveState();
            }  
        }
    }

    [PunRPC]
    public void RequestNextTurn(int currentActor)
    {
        if (!PhotonNetwork.IsMasterClient) 
            return;
        StartNextTurn(currentActor);
    }

    private int GetNextActorNumber(int currentActor)
    {
        var players = PhotonNetwork.PlayerList;
        if (players.Length != 2) return -1;

        return (players[0].ActorNumber == currentActor) ? players[1].ActorNumber : players[0].ActorNumber;
    }

    public void ExecutePhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;
        string phaseName = GetPhaseName(phaseIndex);
        TurnUI.Instance.UpdateTurnInfo(GameManager.Instance.turnCount, phaseName);

        switch (phaseIndex)
        {
            case 1:
                StepPhase(); break;
            case 2:
                MainPhase(); break;
            case 3:
                BattlePhase(); break;
            case 4:
                EndPhase(); break;
        }
    }

    //상대 턴과 페이즈를 내 턴 버튼에도 반영하기 위한 코드
    public void ChangePhaseViaRPC(int phaseIndex)
    {
        if (phaseIndex == 4)
            return;
        photonView.RPC(nameof(RPC_ChangePhase), RpcTarget.All, phaseIndex);
    }

    [PunRPC]
    public void RPC_ChangePhase(int phaseIndex)
    {
        ExecutePhase(phaseIndex);
    }

    private string GetPhaseName(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: return "Draw Phase";
            case 1: return "Step Phase";
            case 2: return "Main Phase";
            case 3: return "Battle Phase";
            case 4: return "End Phase";
            default: return "Unknown Phase";
        }
    }

    private IEnumerator ExecuteDrawPhase()
    {
        TurnUI.Instance.UpdateTurnInfo(GameManager.Instance.turnCount, "Draw Phase");
        GameManager.Instance.DrawCard();

        //엑스트라 드로우 UI 표시
        yield return new WaitForSeconds(0.3f); //약간의 대기
        TurnUI.Instance.ShowExtraDraw((wantsExtra) =>
        {
            if (wantsExtra)
            {
                GameManager.Instance.ExtraDraw();
                //AP1 소모로 추가 드로우
            }
            //currentPhaseIndex = 1;
            ChangePhaseViaRPC(1);
        });
    }

    private void StepPhase()
    {

    }

    private void MainPhase()
    {

    }

    private void BattlePhase() 
    {
        
    }

    private void EndPhase()
    {
        TurnUI.Instance.UpdateTurnInfo(GameManager.Instance.turnCount, "End Phase");
        TurnUI.Instance.HidePhaseButtons();
        ResetAllTempBuffs();
        EndMyTurn();
    }

    private void ResetAllTempBuffs()
    {
        foreach (var slot in MyFieldManager.Instance.myFieldSlots)
        {
            if (slot.HasCard())
            {
                slot.GetPlacedCard().ResetTempBP();
                slot.GetPlacedCard().ResetCount();
            }
        }
        foreach (var slot in OpponentFieldManager.Instance.opponentFieldSlots)
        {
            if (slot.HasCard())
            {
                slot.GetPlacedCard().ResetTempBP();
                slot.GetPlacedCard().ResetCount();
            }
        }
    }
}
