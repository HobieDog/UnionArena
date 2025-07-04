using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckCustomUIManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField deckNameInput;
    [SerializeField] private TMP_Dropdown deckDropdown;
    [SerializeField] private Button addNewDeckButton;
    [SerializeField] private Button saveDeckButton;
    [SerializeField] private Button deleteDeckButton;
    private LobbyManager lobbyManager;

    private void Start()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();
        DeckManager.Instance.UpdateDeckDropdown(deckDropdown);
        deckNameInput.onValueChanged.AddListener(OnDeckNameChanged);
        deckDropdown.onValueChanged.AddListener(OnDeckSelected);
        addNewDeckButton.onClick.AddListener(AddNewDeck);
        saveDeckButton.onClick.AddListener(SaveCurrentDeck);
        deleteDeckButton.onClick.AddListener(DeleteCurrentDeck);

        UpdateDeckDropdown();
    }

    public void OnDeckNameChanged(string newName)
    {
        string oldName = DeckManager.Instance.GetCurrentDeckName();
        if (!string.IsNullOrEmpty(newName) && oldName != newName)
        {
            DeckManager.Instance.RenameDeck(oldName, newName);
            UpdateDeckDropdown();
        }
    }

    public void UpdateDeckDropdown()
    {
        deckDropdown.ClearOptions();
        deckDropdown.AddOptions(DeckManager.Instance.GetSavedDeckNames());
        deckDropdown.value = deckDropdown.options.FindIndex(option => option.text == DeckManager.Instance.GetCurrentDeckName());
        deckDropdown.RefreshShownValue();

        System.GC.Collect();
    }

    private void OnDeckSelected(int index)
    {
        string selectedDeckName = deckDropdown.options[index].text;
        DeckManager.Instance.LoadDeck(selectedDeckName);
        deckNameInput.text = selectedDeckName;
    }

    private void AddNewDeck()
    {
        string newDeckName = $"NewDeck_{DeckManager.Instance.GetSavedDeckNames().Count + 1}";
        DeckManager.Instance.SaveDeckList(newDeckName);
        DeckManager.Instance.LoadDeck(newDeckName);
        UpdateDeckDropdown();
    }

    private void SaveCurrentDeck()
    {
        DeckManager.Instance.SaveDeck(DeckManager.Instance.GetCurrentDeckName());
        DeckManager.Instance.UpdateDeckDropdown(deckDropdown);
        DeckManager.Instance.SaveDecksToJson();
        lobbyManager?.UpdateDeckDropdown();
    }

    private void DeleteCurrentDeck()
    {
        string deckName = DeckManager.Instance.GetCurrentDeckName();

        DeckManager.Instance.DeleteDeck(deckName);
        UpdateDeckDropdown();
        SaveCurrentDeck();

        // 덱 삭제 후 기본 덱 로드
        if (deckDropdown.options.Count > 0)
        {
            OnDeckSelected(0); // 첫 번째 덱 선택
        }
    }
}
