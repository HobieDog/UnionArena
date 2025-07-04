using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject deckMakeUI; // 덱 커스텀 UI
    [SerializeField] private GameObject lobbyUI; // 로비 UI
    [SerializeField] private Button GameStartButton; // 게임시작 버튼
    [SerializeField] private Button DeckCustomButton; // 덱 커스텀 버튼
    [SerializeField] private Button BackButton; // 백 버튼
    [SerializeField] public TMP_Dropdown lobbydeckDropdown; // 덱 리스트 드롭다운
    [SerializeField] private Image warningText; // 경고 메시지 표시
    private MatchmakingManager matchmakingManager;
    private List<List<Card>> allDecks = new List<List<Card>>();


    private void Start()
    {
        ShowLobby(); // 게임 시작 시 로비 화면만 보이도록 설정
        UpdateDeckDropdown(); // 드롭다운

        matchmakingManager = FindObjectOfType<MatchmakingManager>();
        DeckManager.Instance.UpdateDeckDropdown(lobbydeckDropdown);

        lobbydeckDropdown.onValueChanged.AddListener((index) => OnDeckSelected(index)); // 드롭다운
        GameStartButton.onClick.AddListener(ValidateDeck); // 게임시작
        DeckCustomButton.onClick.AddListener(ShowDeckCustom); // 덱 커스텀
        BackButton.onClick.AddListener(OnBackButtonPressed); // 뒤로가기
        warningText.gameObject.SetActive(false); // 시작 시 경고 메시지 숨김
    }

    // 덱 개수 검증 후 게임 시작
    private void ValidateDeck()
    {
        int index = lobbydeckDropdown.value;
        string selectedDeckName = lobbydeckDropdown.options[index].text;

        PlayerPrefs.SetString("SelectedDeckName", selectedDeckName);
        PlayerPrefs.Save();
        Debug.Log($"선택된 덱 저장: {selectedDeckName}");

        List<Card> selectedDeck = DeckManager.Instance.GetDeckByName(selectedDeckName);
        DeckManager.Instance.SetSelectedDeck(selectedDeck);

        int totalCards = DeckManager.Instance.GetTotalDeckSize();

        if (totalCards < DeckManager.MAX_DECK_SIZE)
        {
            warningText.gameObject.SetActive(true); // 경고 메시지 표시
            StartCoroutine(HideWarningAfterDelay());
        }
        else
        {
            StartMatchmaking(); // 정상적으로 게임 시작
        }
    }

    private void StartMatchmaking()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.StartGame();
        }
        else
        {
            Debug.LogError("매치메이킹이 안됨");
        }
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        warningText.gameObject.SetActive(false);
    }

    public void UpdateDeckDropdown()
    {
        lobbydeckDropdown.ClearOptions();
        lobbydeckDropdown.AddOptions(DeckManager.Instance.GetSavedDeckNames());
        allDecks = DeckManager.Instance.GetAllDecks();
        lobbydeckDropdown.value = lobbydeckDropdown.options.FindIndex(option => option.text == DeckManager.Instance.GetCurrentDeckName());
        lobbydeckDropdown.RefreshShownValue();
    }

    private void OnDeckSelected(int index)
    {
        string selectedDeckName = lobbydeckDropdown.options[index].text;

        PlayerPrefs.SetString("SelectedDeckName", selectedDeckName);
        PlayerPrefs.Save();

        DeckManager.Instance.LoadDeck(selectedDeckName);
        Debug.Log($"선택한 덱: {selectedDeckName} 저장 완료");
    }

    public void ShowLobby()
    {
        deckMakeUI.SetActive(false); // 로비 화면
        lobbyUI.SetActive(true);
    }

    public void ShowDeckCustom()
    {
        deckMakeUI.SetActive(true); // 덱 커스텀 UI 활성화
        lobbyUI.SetActive(false);
    }

    private void OnBackButtonPressed()
    {
        ShowLobby(); // 로비로 이동
    }
}
