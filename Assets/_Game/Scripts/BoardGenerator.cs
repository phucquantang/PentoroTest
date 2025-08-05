using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using UnityEngine;
using UnityEngine.UI;

public class BoardGenerator : MonoBehaviour
{
    public List<Tile> AllTiles = new List<Tile>();
    public int Columns = 9;

    [SerializeField] private BoardController _boardController;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private int _initialNumberCount = 27;
    [SerializeField] private int _initialSize = 90;
    [SerializeField] private int _stage = 1;
    [SerializeField] private Transform _gridContent;
    [SerializeField] private Dictionary<GemType, int> _gemTargets = new Dictionary<GemType, int>();

    private GameMode _gameMode = GameMode.Normal;
    private ObjectPool<Tile> _tilePool = new ObjectPool<Tile>();
    private CSPSolver _cspSolver;
    private float _addTileSpeed = 0.001f;

    private const int YIELD_FREQUENCY = 10;

    private void OnEnable()
    {
        EventDispatch.AddListener(gameObject, OnGemMatched, EventDispatchName.GemMatched);
    }

    private void OnDisable()
    {
        EventDispatch.RemoveListener(OnGemMatched, EventDispatchName.GemMatched);
    }

    private void OnDestroy()
    {
        _tilePool.Clear();
        _cspSolver?.Clear();
    }

    // private void Start()
    // {
    //     // Uncomment these lines for testing with predefined numbers
    //     ClearBoard();
    //     var inputNumbers = new List<int> { 4, 6, 1, 8, 2, 3, 2, 9, 2, 1, 1, 8, 5, 5, 1, 5, 6, 4, 8, 9, 7, 2, 5, 8, 6, 3, 2, 1, 3, 2, 6, 4, 2, 3, 1, 6, 9, 7, 4, 5, 4, 2, 5, 5, 8, 2, 3, 9, 2, 7, 9, 6, 3, 3 };
    //     SetBoardFromInput(inputNumbers);
    // }

    public void Initialize()
    {
        _cspSolver = new CSPSolver(Columns, _initialNumberCount);
        _gameMode = GameManager.Instance.GameMode;
        StartCoroutine(GenerateBoard());
    }

    public void RegenerateLevel() => StartCoroutine(GenerateBoard());

    public void NextStage()
    {
        _stage++;
        GameUIManager.Instance.UpdateStageText(_stage);
        RegenerateLevel();
    }

    public void AppendActiveTiles()
    {
        StartCoroutine(AppendActiveTilesCoroutine());
    }

    public void CheckForLoss()
    {
        if (!HasAnyValidMoves())
        {
            if (GameUIManager.CurrentAddNumberCount <= 0) EventDispatch.Dispatch(EventDispatchName.Lose);
        }
    }

    private void SetBoardFromInput(List<int> numbers)
    {
        StartCoroutine(CreateBoardFromNumbers(numbers));
    }

    private void ClearBoard()
    {
        for (int i = 0; i < AllTiles.Count; i++)
        {
            var tile = AllTiles[i];
            if (tile != null)
            {
                _tilePool.Return(tile);
            }
        }

        AllTiles.Clear();
    }

    private IEnumerator CreateBoardFromNumbers(List<int> numbers)
    {
        AllTiles.Clear();

        var yieldCounter = 0;

        for (int i = 0; i < _initialSize; i++)
        {
            var tile = _tilePool.Get(_tilePrefab, _gridContent);

            if (i < numbers.Count)
            {
                tile.Initialize(numbers[i], true);
            }
            else
            {
                tile.Initialize(0, false);
            }

            AllTiles.Add(tile);

            if (++yieldCounter >= YIELD_FREQUENCY)
            {
                yieldCounter = 0;
                yield return null;
            }
        }

        if (_gameMode == GameMode.Gem)
        {
            yield return StartCoroutine(GenerateGemsForNumbers(numbers));
        }
    }

    private IEnumerator GenerateBoard()
    {
        if (_gameMode == GameMode.Gem)
        {
            _gemTargets = GameManager.Instance.GemTargets;
        }

        ClearBoard();
        yield return StartCoroutine(CreateTiles());

        _cspSolver.Initialize(_initialNumberCount);
        _cspSolver.PreCalculateNeighbors(_initialNumberCount);
        _cspSolver.GenerateRequiredPairs(_stage);
        _cspSolver.ApplyInitialConstraints();

        var success = false;
        int[] solution = null;

        yield return StartCoroutine(_cspSolver.SolveCSP((result, assignment) =>
        {
            success = result;
            solution = assignment;
        }));

        if (success && solution != null)
        {
            for (int i = 0; i < _initialNumberCount; i++)
            {
                AllTiles[i].Value = solution[i];
            }

            if (_gameMode == GameMode.Gem)
            {
                var activeNumbers = solution.Take(_initialNumberCount).ToList();
                yield return StartCoroutine(GenerateGemsForNumbers(activeNumbers));
            }
        }
        else
        {
            Debug.Log("Failed");
        }
    }

    private IEnumerator CreateTiles()
    {
        AllTiles.Clear();
        var yieldCounter = 0;

        for (int i = 0; i < _initialSize; i++)
        {
            var tile = _tilePool.Get(_tilePrefab, _gridContent);

            if (i < _initialNumberCount)
            {
                tile.Initialize(0, true);
            }
            else
            {
                tile.Initialize(0, false);
            }

            AllTiles.Add(tile);

            if (++yieldCounter >= YIELD_FREQUENCY)
            {
                yieldCounter = 0;
                yield return null;
            }
        }
    }

    private IEnumerator AppendActiveTilesCoroutine()
    {
        // Step 1: Collect all values > 0
        var valuesToAppend = new List<int>();
        foreach (var tile in AllTiles)
        {
            if (tile.IsActive && tile.Value > 0)
            {
                valuesToAppend.Add(tile.Value);
            }
        }

        if (valuesToAppend.Count == 0)
        {
            yield break;
        }

        // Step 2: Calculate tiles needed
        var currentActiveTiles = AllTiles.Count(t => t.IsActive);
        var requiredTilesForAppend = valuesToAppend.Count;
        var totalTilesNeeded = currentActiveTiles + requiredTilesForAppend;

        if (totalTilesNeeded > 81)
        {
            _scrollRect.enabled = true;
        }

        while (AllTiles.Count < totalTilesNeeded + 18)
        {
            for (int i = 0; i < Columns; i++)
            {
                var newTile = _tilePool.Get(_tilePrefab, _gridContent);
                newTile.Initialize(0, false);
                AllTiles.Add(newTile);
            }
        }

        if (_scrollRect.enabled)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        // Step 3: Activate inactive tiles with the values to append
        var inactiveTiles = AllTiles.Where(t => !t.IsActive).Take(valuesToAppend.Count).ToList();
        for (int i = 0; i < valuesToAppend.Count && i < inactiveTiles.Count; i++)
        {
            inactiveTiles[i].Initialize(valuesToAppend[i], true);
            yield return new WaitForSeconds(_addTileSpeed);
            if (i % YIELD_FREQUENCY == 0)
                yield return null;
        }

        if (_gameMode == GameMode.Gem)
        {
            yield return StartCoroutine(GenerateGemsForNumbers(valuesToAppend));
        }

        GameUIManager.IsAddingNumbers = false;
    }

    #region Gem Generation Logic
    private void OnGemMatched(object param)
    {
        if (param is not GemType type) return;
        if (_gemTargets[type] <= 0) return;

        _gemTargets[type]--;
        if (_gemTargets.All(t => t.Value <= 0))
        {
            EventDispatch.Dispatch(EventDispatchName.Win, GameManager.Instance.GemTargets);
        }
    }

    private bool HasAnyValidMoves()
    {
        var count = AllTiles.Count;

        for (int i = 0; i < count; i++)
        {
            var tile1 = AllTiles[i];
            if (!tile1.IsActive || tile1.Value <= 0) continue;

            for (int j = i + 1; j < count; j++)
            {
                var tile2 = AllTiles[j];
                if (!tile2.IsActive || tile2.Value <= 0) continue;

                bool valuesMatch = tile1.Value == tile2.Value || tile1.Value + tile2.Value == 10;

                if (valuesMatch && _boardController.IsMatchValid(tile1, tile2, out List<Tile> blockingTiles))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator GenerateGemsForNumbers(List<int> numbers)
    {
        var availableGemTypes = _gemTargets.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
        if (availableGemTypes.Count == 0) yield break;

        float gemChancePercent = UnityEngine.Random.Range(5f, 7f); // X%: Random 5-7%
        var guaranteedGemInterval = Mathf.CeilToInt((numbers.Count + 1) / 2f); // Y: (count+1)/2
        var maxGemsThisRound = _gemTargets.Count(); // Z: Gem targets count

        if (maxGemsThisRound <= 0)
        {
            yield break;
        }

        var newlyActiveTiles = GetNewlyActiveTiles(numbers);
        if (newlyActiveTiles.Count == 0)
        {
            yield break;
        }

        // Generate gems
        var gemTiles = new List<Tile>();
        var tilesSinceLastGem = 0;
        var gemsCreated = 0;

        for (int i = 0; i < newlyActiveTiles.Count && gemsCreated < maxGemsThisRound; i++)
        {
            var tile = newlyActiveTiles[i];
            var shouldCreateGem = false;

            if (UnityEngine.Random.Range(0f, 100f) < gemChancePercent)
            {
                shouldCreateGem = true;
            }

            tilesSinceLastGem++;
            if (tilesSinceLastGem >= guaranteedGemInterval)
            {
                shouldCreateGem = true;
                tilesSinceLastGem = 0;
            }

            if (shouldCreateGem)
            {
                // Pick a random gem type with > 0 count
                var gemType = availableGemTypes[UnityEngine.Random.Range(0, availableGemTypes.Count)];

                // Check if this gem can be placed without creating matche
                if (CanPlaceGemWithoutMatches(tile, gemType, gemTiles))
                {
                    if (gemType == GemType.Gem1)
                        tile.SetAsGem(GameManager.Instance.GemSO.gem1Sprite, gemType);
                    else if (gemType == GemType.Gem2)
                        tile.SetAsGem(GameManager.Instance.GemSO.gem2Sprite, gemType);
                    else if (gemType == GemType.Gem3)
                        tile.SetAsGem(GameManager.Instance.GemSO.gem3Sprite, gemType);

                    gemTiles.Add(tile);
                    gemsCreated++;
                    tilesSinceLastGem = 0;
                }
            }

            if (i % YIELD_FREQUENCY == 0)
                yield return null;
        }
    }

    private List<Tile> GetNewlyActiveTiles(List<int> numbers)
    {
        var activeTiles = AllTiles.Where(t => t.IsActive && t.Value > 0);
        var activeTileCount = activeTiles.Count();
        var startIndex = Math.Max(0, activeTileCount - numbers.Count);

        return activeTiles.Skip(startIndex).Take(numbers.Count).ToList();
    }

    private bool CanPlaceGemWithoutMatches(Tile gemTile, GemType gemType, List<Tile> existingGemTiles)
    {
        var gemTileIndex = GetTileIndex(gemTile);
        var gemValue = gemTile.Value;

        foreach (var existingGem in existingGemTiles)
        {
            if (existingGem.Gem != gemType)
                continue;

            var existingGemIndex = GetTileIndex(existingGem);
            var existingGemValue = existingGem.Value;

            if (WouldTilesMatch(gemTileIndex, gemValue, existingGemIndex, existingGemValue))
            {
                return false;
            }
        }

        return true;
    }

    private bool WouldTilesMatch(int index1, int value1, int index2, int value2)
    {
        if (value1 != value2 || value1 + value2 != 10)
        {
            return false;
        }

        if (AreIndicesAdjacent(index1, index2))
        {
            return true;
        }

        return false;
    }

    private bool AreIndicesAdjacent(int index1, int index2)
    {
        var row1 = index1 / Columns;
        var col1 = index1 % Columns;
        var row2 = index2 / Columns;
        var col2 = index2 % Columns;

        var rowDiff = Math.Abs(row1 - row2);
        var colDiff = Math.Abs(col1 - col2);

        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    #endregion

    public Tile GetTileAt(int index)
    {
        return index >= 0 && index < AllTiles.Count ? AllTiles[index] : null;
    }

    public int GetTileIndex(Tile tile)
    {
        return AllTiles.IndexOf(tile);
    }
}