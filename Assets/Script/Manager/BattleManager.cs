using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class BattleManager : MonoBehaviourPun
{
    public static BattleManager Instance;
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

    public void InitiateAttack(GameBaseCard attacker)
    {
        int attackerSlotIndex = MyFieldManager.Instance.GetSlotIndex(attacker.CurrentSlot);
        photonView.RPC(nameof(RequestDefense), RpcTarget.Others, attackerSlotIndex);
    }

    [PunRPC]
    public void RequestDefense(int attackerSlotIndex)
    {
        FieldSlotUI attackerSlot = OpponentFieldManager.Instance.opponentFieldSlots[attackerSlotIndex];
        GameBaseCard attacker = attackerSlot.GetPlacedCard();

        var defenderCandidates = MyFieldManager.Instance.myFieldSlots
                        .Where(slot => slot.IsMyFrontLine() && slot.HasCard()).ToList();
        if (defenderCandidates.Count == 0)
        {
            LifeZoneManager.Instance.ShowLifeSelection();
            return;
        }

        TriggerUIManager.Instance.ShowAttackerPreview(attacker);
        TriggerUIManager.Instance.ShowYesNo("수비 하시겠습니까?", onYes: () =>
        {
            TriggerUIManager.Instance.HideAttackerPreview();
            TriggerUIManager.Instance.ShowCardSelection(defenderCandidates.Select(s => s.GetPlacedCard()).ToList(), selectedDefenderCard =>
            {
                ResolveBattle(attacker, selectedDefenderCard);
            });
        },
        onNo: () =>
        {
            TriggerUIManager.Instance.HideAttackerPreview();
            attacker.RegisterAttack();
            photonView.RPC(nameof(RPC_ApplyLifeDamageToDefender), RpcTarget.Others, attackerSlotIndex);
        });
    }

    [PunRPC]
    public void RPC_ApplyLifeDamageToDefender(int attackerSlotIndex)
    {
        FieldSlotUI attackerSlot = MyFieldManager.Instance.myFieldSlots[attackerSlotIndex];
        GameBaseCard attacker = attackerSlot.GetPlacedCard();
        LifeZoneManager.Instance.ShowLifeSelection(); //공격자 기준에서 실행됨
        attacker.RegisterAttack();
    }

    private void ResolveBattle(GameBaseCard attacker, GameBaseCard defender)
    {
        int attackerBP = attacker.GetFinalBP();
        int defenderBP = defender.GetFinalBP();

        bool isAttackerWin = attackerBP >= defenderBP;

        int attackerSlotIndex = OpponentFieldManager.Instance.GetSlotIndex(attacker.CurrentSlot);
        int defenderSlotIndex = MyFieldManager.Instance.GetSlotIndex(defender.CurrentSlot);

        if (isAttackerWin)
        {
            defender.CurrentSlot.ClearSlot(false);
            Destroy(defender.gameObject);
        }
        else
        {
            defender.RegisterDefense();
        }

        attacker.RegisterAttack();
        photonView.RPC(nameof(SyncBattleResult), RpcTarget.Others, attackerSlotIndex, defenderSlotIndex, isAttackerWin);
    }

    [PunRPC]
    public void SyncBattleResult(int attackerSlotIndex, int defenderSlotIndex, bool isAttackerWin)
    {
        FieldSlotUI attackerSlot = MyFieldManager.Instance.myFieldSlots[attackerSlotIndex];
        FieldSlotUI defenderSlot = OpponentFieldManager.Instance.opponentFieldSlots[defenderSlotIndex];

        GameBaseCard attacker = attackerSlot.GetPlacedCard();
        GameBaseCard defender = defenderSlot.GetPlacedCard();

        if (isAttackerWin)
        {
            GameBaseCard stacked = defender.GetStackedCard();
            if (stacked != null)
            {
                GraveyardManager.Instance.SendToGrave(stacked.cardData, false);
                Destroy(stacked.gameObject);
            }

            defenderSlot.ClearSlot(false);
            GraveyardManager.Instance.SendToGrave(defender.cardData, false);
            Destroy(defender.gameObject);

            //임팩트 체크
            if (attacker.HasEffect(ECardEffect.Impact) && !defender.HasEffect(ECardEffect.DisImpact))
            {
                LifeZoneManager.Instance.ShowLifeSelection();
            }
        }
        else
        {
            defender.RegisterDefense();
        }
        attacker.RegisterAttack();
    }

    [PunRPC]
    public void SyncTempBPBuff(int slotIndex, int newTempBP)
    {
        if (slotIndex < 0 || slotIndex >= OpponentFieldManager.Instance.opponentFieldSlots.Count)
            return;

        var targetSlot = OpponentFieldManager.Instance.opponentFieldSlots[slotIndex];
        if (!targetSlot.HasCard()) return;

        var targetCard = targetSlot.GetPlacedCard();
        targetCard.SetTempBPDirectly(newTempBP);
    }
}
