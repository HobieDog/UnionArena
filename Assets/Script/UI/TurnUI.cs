using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnUI : MonoBehaviour
{
    public static TurnUI Instance;

    //�� ��ư �� ������ �г�
    [SerializeField] private GameObject phasePanel;
    [SerializeField] private Button turnButton;
    [SerializeField] private List<Button> phaseButtons;
    [SerializeField] private TMP_Text turnNumberText;
    [SerializeField] private TMP_Text phaseNameText;

    //����Ʈ�� ��ο� �г�
    [SerializeField] private GameObject extraDrawPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    private System.Action<bool> onExtraDrawComplete;

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

        extraDrawPanel.SetActive(false);
        phasePanel.SetActive(false);
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        turnButton.onClick.AddListener(OnTurnButtonClicked);

        for (int i = 0; i < phaseButtons.Count; i++)
        {
            int index = i;
            phaseButtons[i].onClick.AddListener(() => OnPhaseButtonClicked(index));
        }

        yesButton.onClick.AddListener(() =>
        {
            extraDrawPanel.SetActive(false);
            onExtraDrawComplete?.Invoke(true);
        });

        noButton.onClick.AddListener(() =>
        {
            extraDrawPanel.SetActive(false);
            onExtraDrawComplete?.Invoke(false);
        });
    }

    private void OnTurnButtonClicked()
    {
        if (!TurnManager.Instance.isMyTurn)
            return;

        phasePanel.SetActive(true);
        UpdatePhaseButtons();
    }

    private void OnPhaseButtonClicked(int phaseIndex)
    {
        if (TurnManager.Instance.isMyTurn && phaseIndex > TurnManager.Instance.currentPhaseIndex)
        {
            TurnManager.Instance.ChangePhaseViaRPC(phaseIndex);
            TurnManager.Instance.ExecutePhase(phaseIndex);
            phasePanel.SetActive(false);
            UpdatePhaseButtons();
        }
    }

    public void HidePhaseButtons()
    {
        phasePanel.SetActive(false);
        foreach (var btn in phaseButtons)
            btn.interactable = false;
    }

    public void UpdateTurnInfo(int turn, string phase)
    {
        turnNumberText.text = $"Turn {turn}";
        phaseNameText.text = $"{phase}";
    }

    private void UpdatePhaseButtons()
    {
        int currentPhase = TurnManager.Instance.currentPhaseIndex;

        for (int i = 0; i < phaseButtons.Count; i++)
        {
            phaseButtons[i].interactable = TurnManager.Instance.isMyTurn && i > currentPhase;
        }
    }

    //����Ʈ�� ��ο�
    public void ShowExtraDraw(System.Action<bool> onComplete)
    {
        extraDrawPanel.SetActive(true);
        onExtraDrawComplete = onComplete;
    }
}
