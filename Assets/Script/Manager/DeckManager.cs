using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Newtonsoft.Json;

[System.Serializable]
public class DeckData
{
    public string title;
    public List<int> cardIds = new List<int>(); // 카드 ID 리스트
    public List<int> cardCounts = new List<int>(); // 카드 개수 리스트
}

[System.Serializable]
public class DeckEntry
{
    public string deckName;
    public DeckData deckData;

    public DeckEntry(string name, DeckData data)
    {
        deckName = name;
        deckData = data;
    }
}

[System.Serializable]
public class DeckSaveWrapper
{
    public List<DeckEntry> decks;
}

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Transform deckListParent; // DeckCardView의 Content
    [SerializeField] private DeckCardUI deckCardPrefab; // DeckCard 프리팹
    [SerializeField] private Text deckCountText;

    private Dictionary<int, int> deckData = new Dictionary<int, int>(); // 어떤 카드가 몇 장 들어갔는가
    private Dictionary<int, DeckCardUI> deckCards = new Dictionary<int, DeckCardUI>();
    private Dictionary<string, DeckData> savedDecks = new Dictionary<string, DeckData>();
    public const int MAX_DECK_SIZE = 20; // 덱 최대 개수
    private string currentDeckName = "Default Deck"; // 현재 선택된 덱 이름

    // 게임에 들고간 덱
    private List<Card> selectedDeck = new List<Card>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            LoadDecks();
        }
        else
        {
            Destroy(gameObject); 
        }

        LoadDecks();

        if (GetSavedDeckNames().Count == 0)
        {
            SaveDeck("기본 덱"); // 게임 처음 실행 시 기본 덱 추가
        }
    }

    //카드 추가 제거
    public void AddCardToDeck(Card card)
    {
        if (GetTotalDeckSize() >= MAX_DECK_SIZE)
        {
            return;
        }

        if (deckCards.ContainsKey(card.cardId))
        {
            if (deckData[card.cardId] >= 4)
            {
                return;
            }
            deckData[card.cardId]++;
            deckCards[card.cardId].IncreaseCardCount(); // 이미 있는 카드라면 개수만 증가
        }
        else
        {
            deckData[card.cardId] = 1;
            DeckCardUI newCard = Instantiate(deckCardPrefab, deckListParent);
            newCard.Init(card);
            deckCards.Add(card.cardId, newCard);
        }

        UpdateDeckCount();
    }
    public void RemoveCardFromDeck(Card card)
    {
        if (deckData.ContainsKey(card.cardId))
        {
            deckData[card.cardId]--;

            if (deckData.ContainsKey(card.cardId) && deckData[card.cardId] <= 0)
            {
                deckData.Remove(card.cardId);

                if (deckCards.ContainsKey(card.cardId))
                {
                    Destroy(deckCards[card.cardId].gameObject);
                    deckCards.Remove(card.cardId);
                }
            }
            else
            {
                deckCards[card.cardId].DecreaseCardCount();
            }
        }

        UpdateDeckCount();
    }

    // 덱에 추가된 카드 갯수를 세고 체크
    public void UpdateCardCount(int cardId, int count)
    {
        deckData[cardId] = count; // 바로 저장 가능
        UpdateDeckCount();
    }

    public void UpdateDeckCount()
    {
        int totalCards = GetTotalDeckSize();

        deckCountText.text = $"{totalCards}";

    }

    public int GetTotalDeckSize()
    {
        int total = 0;
        foreach (var count in deckData.Values)
        {
            total += count;
        }
        return total;
    }

    // 덱 이름 및 내용
    public Dictionary<int, int> GetDeckData()
    {
        return new Dictionary<int, int>(deckData); // 현재 덱의 카드 ID와 개수를 반환
    }

    public List<string> GetSavedDeckNames()
    {
        return new List<string>(savedDecks.Keys);
    }

    // 덱 세이브 로드 하기
    public void SaveDeck(string deckName)
    {
        if (string.IsNullOrEmpty(deckName))
        {
            return;
        }

        DeckData deckData = new DeckData();
        foreach (var pair in GetDeckData()) // 현재 덱 정보 가져오기
        {
            deckData.cardIds.Add(pair.Key);
            deckData.cardCounts.Add(pair.Value);
        }

        savedDecks[deckName] = deckData; // 덱 저장
        SaveAllDecks(); // 전체 덱 저장
    }

    public void SaveDecksToJson()
    {
        //string path = Path.Combine(Application.persistentDataPath, "decks.json");

        //DeckSaveWrapper wrapper = new DeckSaveWrapper
        //{
        //    decks = new List<DeckEntry>()
        //};

        //foreach (var entry in savedDecks)
        //{
        //    wrapper.decks.Add(new DeckEntry(entry.Key, entry.Value));
        //}

        //string json = JsonUtility.ToJson(wrapper, true);
        //File.WriteAllText(path, json);
        string path = Path.Combine(Application.persistentDataPath, "decks.json");

        DeckSaveWrapper wrapper = new DeckSaveWrapper
        {
            decks = new List<DeckEntry>()
        };

        foreach (var entry in savedDecks)
        {
            wrapper.decks.Add(new DeckEntry(entry.Key, entry.Value));
        }

        try
        {
            string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            File.WriteAllText(path, json);

            if (File.Exists(path))
            {
                string verifyContent = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError($"파일 저장 실패 경로: {path}");
            }
        }
        catch (Exception exc)
        {
            Debug.LogError($"JSON 저장 중 오류 발생: {exc.Message}\n{exc.StackTrace}");
        }
    }

    public void LoadDeck(string deckName)
    {
        if (savedDecks.ContainsKey(deckName))
        {
            currentDeckName = deckName; // 현재 선택된 덱 업데이트
            DeckData loadedDeck = savedDecks[deckName];

            ClearDeck();

            for (int i = 0; i < loadedDeck.cardIds.Count; i++)
            {
                int cardId = loadedDeck.cardIds[i];
                int count = loadedDeck.cardCounts[i];

                deckData[cardId] = count; // 데이터 업데이트

                Card card = CardDatabase.Instance.GetCardById(cardId);
                if (card != null)
                {
                    DeckCardUI newCard = Instantiate(deckCardPrefab, deckListParent);
                    newCard.Init(card);
                    newCard.SetCardCount(count); // UI 업데이트
                    deckCards.Add(cardId, newCard);
                }
            }
            UpdateDeckCount();
        }
    }

    public void LoadDecks()
    {
        string path = Path.Combine(Application.persistentDataPath, "decks.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            DeckSaveWrapper wrapper = JsonUtility.FromJson<DeckSaveWrapper>(json);

            savedDecks.Clear();
            foreach (DeckEntry entry in wrapper.decks)
            {
                savedDecks[entry.deckName] = entry.deckData;
            }
        }
    }

    //덱 삭제하기
    public void DeleteDeck(string deckName)
    {
        if (savedDecks.ContainsKey(deckName))
        {
            savedDecks.Remove(deckName);
            PlayerPrefs.DeleteKey($"Deck_{deckName}");
            SaveAllDecks();
        }
    }

    //덱 전체(여러개) 저장하기
    [System.Serializable]
    public class DeckSaveData
    {
        public List<DeckEntry> decks = new List<DeckEntry>();

        public DeckSaveData(Dictionary<string, DeckData> savedDecks)
        {
            foreach (var entry in savedDecks)
            {
                decks.Add(new DeckEntry(entry.Key, entry.Value));
            }
        }
    }

    public void SaveAllDecks()
    {
        if (savedDecks.Count == 0) return;

        DeckSaveData data = new DeckSaveData(savedDecks);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SavedDecks", json);
        PlayerPrefs.Save();
    }

    // 덱 이름 고치기
    public void RenameDeck(string oldName, string newName)
    {
        if (savedDecks.ContainsKey(oldName) && !savedDecks.ContainsKey(newName))
        {
            DeckData deckData = savedDecks[oldName];
            savedDecks.Remove(oldName);
            savedDecks[newName] = deckData;

            if (currentDeckName == oldName) // 현재 선택된 덱이 변경된 경우
            {
                currentDeckName = newName;
            }

            SaveAllDecks();
        }
    }

    public List<string> GetSavedDecks()
    {
        List<string> deckNames = new List<string>();

        foreach (var key in PlayerPrefs.GetString("SavedDecks", "").Split(','))
        {
            if (!string.IsNullOrEmpty(key))
                deckNames.Add(key);
        }
        return deckNames;
    }

    public void SaveDeckList(string newDeckName)
    {
        if (!savedDecks.ContainsKey(newDeckName))
        {
            savedDecks[newDeckName] = new DeckData();
            SaveAllDecks(); // 덱 리스트와 데이터 전체 저장
        }
    }

    // 덱 커스텀, 로비 드롭다운 동시 업데이트
    public void UpdateDeckDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        List<string> deckNames = GetSavedDeckNames();

        if (deckNames.Count == 0)
        {
            deckNames.Add("기본 덱");
            SaveDeck("기본 덱");
        }

        dropdown.AddOptions(deckNames);
        dropdown.value = dropdown.options.FindIndex(option => option.text == GetCurrentDeckName());
        dropdown.RefreshShownValue();
    }

    public void ClearDeck()
    {
        foreach (var cardUI in deckCards.Values)
        {
            if (cardUI != null)
            {
                DestroyImmediate(cardUI.gameObject); // 삭제
            }
        }

        deckCards.Clear(); // UI Dictionary 초기화
        deckData.Clear(); // 데이터 Dictionary 초기화
        UpdateDeckCount(); // UI 업데이트
    }

    // 로비화면 덱 선택 모든 덱 반환
    public List<List<Card>> GetAllDecks()
    {
        List<List<Card>> deckLists = new List<List<Card>>();

        foreach (var deck in savedDecks.Values)
        {
            List<Card> cardList = new List<Card>();

            for (int i = 0; i < deck.cardIds.Count; i++)
            {
                int cardId = deck.cardIds[i];
                int count = deck.cardCounts[i];

                // ID를 기반으로 CardDatabase에서 카드 정보 가져오기
                Card card = CardDatabase.Instance.GetCardById(cardId);

                if (card != null)
                {
                    for (int j = 0; j < count; j++)
                    {
                        cardList.Add(card);
                    }
                }
            }
            deckLists.Add(cardList);
        }

        return deckLists;
    }

    public string GetCurrentDeckName()
    {
        return currentDeckName;
    }

    // 게임에서 선택한 덱 데이터 가져오기
    public void SetSelectedDeck(List<Card> deck)
    {
        selectedDeck = deck;
    }

    public List<Card> GetSelectedDeck()
    {
        string selectedDeckName = PlayerPrefs.GetString("SelectedDeckName", "");

        if (string.IsNullOrEmpty(selectedDeckName) || !savedDecks.ContainsKey(selectedDeckName))
        {
            return null;
        }

        List<Card> loadedDeck = new List<Card>();
        DeckData deckData = savedDecks[selectedDeckName];

        for (int i = 0; i < deckData.cardIds.Count; i++)
        {
            int cardId = deckData.cardIds[i];
            int count = deckData.cardCounts[i];

            Card card = CardDatabase.Instance.GetCardById(cardId);
            if (card != null)
            {
                for (int j = 0; j < count; j++)
                {
                    loadedDeck.Add(card);
                }
            }
        }
        return loadedDeck;
    }

    public List<Card> GetDeckByName(string deckName)
    {
        List<Card> loadedDeck = new List<Card>();

        if (!savedDecks.ContainsKey(deckName))
        {
            Debug.LogError($"{deckName}이 존재하지 않습니다.");
            return loadedDeck;
        }

        DeckData deckData = savedDecks[deckName];
        for (int i = 0; i < deckData.cardIds.Count; i++)
        {
            int cardId = deckData.cardIds[i];
            int count = deckData.cardCounts[i];

            Card card = CardDatabase.Instance.GetCardById(cardId);
            if (card != null)
            {
                for (int j = 0; j < count; j++)
                {
                    loadedDeck.Add(card);
                }
            }
        }

        return loadedDeck;
    }
}
