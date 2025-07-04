using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TriggerUIManager : MonoBehaviour
{
    public static TriggerUIManager Instance;

    [Header("Yes/No 다이얼로그")]
    [SerializeField] private GameObject yesNoPanel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("카드 선택 UI")]
    [SerializeField] private GameObject cardSelectionPanel;
    [SerializeField] private Transform cardSelectionContent;
    [SerializeField] private GameObject cardButtonPrefab;

    [Header("공격 카드 UI")]
    [SerializeField] private GameObject attackerPreviewPanel;
    [SerializeField] private Image attackerCardImage;
    [SerializeField] private TextMeshProUGUI attackerBPText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowYesNo(string text, Action onYes, Action onNo)
    {
        yesNoPanel.SetActive(true);
        dialogText.text = text;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() =>
        {
            yesNoPanel.SetActive(false);
            onYes?.Invoke();
        });

        noButton.onClick.AddListener(() =>
        {
            yesNoPanel.SetActive(false);
            onNo?.Invoke();
        });
    }

    public void ShowCardSelection(List<GameBaseCard> cards, Action<GameBaseCard> onSelected)
    {
        cardSelectionPanel.SetActive(true);

        foreach (Transform child in cardSelectionContent)
            Destroy(child.gameObject);

        foreach (var card in cards)
        {
            GameObject cardBtn = Instantiate(cardButtonPrefab, cardSelectionContent);
            cardBtn.GetComponentInChildren<Image>().sprite = card.GetArtwork(); // 카드 이미지
            cardBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                cardSelectionPanel.SetActive(false);
                onSelected?.Invoke(card);
            });
        }
    }

    public void ShowAttackerPreview(GameBaseCard attacker)
    {
        attackerPreviewPanel.SetActive(true);
        attackerCardImage.sprite = attacker.GetArtwork();
        attackerBPText.text = ($"{attacker.GetFinalBP()}");
    }

    public void HideAttackerPreview()
    {
        attackerPreviewPanel.SetActive(false);
    }
}
