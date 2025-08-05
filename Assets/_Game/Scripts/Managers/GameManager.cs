using System.Collections.Generic;
using System.Linq;
using Common;
using UnityEngine;

public enum GameMode
{
    Normal,
    Gem,
}

public enum GemType
{
    Gem1,
    Gem2,
    Gem3
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Dictionary<GemType, int> GemTargets = new Dictionary<GemType, int>();
    public GameMode GameMode;
    public GemSO GemSO;
    public int AddNumberCount = 6;

    [SerializeField] private BoardGenerator _boardGenerator;

    private List<GemType> _allGemTypes = new List<GemType>
    {
        GemType.Gem1,
        GemType.Gem2,
        GemType.Gem3
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnGameplaySceneLoaded()
    {
        _boardGenerator = FindFirstObjectByType<BoardGenerator>();

        if (_boardGenerator != null)
        {
            _boardGenerator.Initialize();
            EventDispatch.Dispatch(EventDispatchName.GameStart);
            return;
        }
    }

    public void LoadGameplayScene(string mode)
    {
        LoadingManager.Instance.LoadScene("Gameplay");

        if (mode == "Gem")
        {
            GameMode = GameMode.Gem;
            GemTargets = GenerateRandomGemTargets();
        }
        else
        {
            GameMode = GameMode.Normal;
        }
    }

    public Dictionary<GemType, int> GenerateRandomGemTargets()
    {
        var gemTargets = new Dictionary<GemType, int>();
        var shuffledGemTypes = _allGemTypes.OrderBy(g => Random.value).ToList();

        var gemCount = Random.Range(1, shuffledGemTypes.Count + 1);

        for (int i = 0; i < gemCount; i++)
        {
            var type = shuffledGemTypes[i];
            var targetAmount = Random.Range(3, 5);
            gemTargets[type] = targetAmount;
        }

        foreach (var pair in gemTargets)
        {
            Debug.Log($"Gem Target: {pair.Key} -> {pair.Value}");
        }

        return gemTargets;
    }

    public void NewGame()
    {
        GemTargets.Clear();

        if (GameMode == GameMode.Gem)
        {
            GemTargets = GenerateRandomGemTargets();
        }

        _boardGenerator.RegenerateLevel();
        EventDispatch.Dispatch(EventDispatchName.GameStart);

    }
}
