using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Common;

public class BoardController : MonoBehaviour
{
    [SerializeField] private BoardGenerator _boardGenerate;

    private Tile _firstSelected;
    private Tile _secondSelected;
    private int _score;
    private int _highScore;

    private List<int> _rowsToProcess = new List<int>();
    private const int Columns = 9;

    private void OnEnable()
    {
        EventDispatch.AddListener(gameObject, OnTileClicked, EventDispatchName.TileClicked);
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnDisable()
    {
        EventDispatch.RemoveAllListener(gameObject);
    }

    public void OnTileClicked(object param)
    {
        if (param is not Tile tile || !tile.IsActive) return;

        if (_firstSelected == null)
        {
            _firstSelected = tile;
            return;
        }

        if (_firstSelected == tile)
        {
            _firstSelected.Deselect();
            _firstSelected = null;
            return;
        }

        _secondSelected = tile;

        var isValueMatch = (_firstSelected.Value == _secondSelected.Value) ||
                           (_firstSelected.Value + _secondSelected.Value == 10);
        if (isValueMatch)
        {
            if (IsMatchValid(_firstSelected, _secondSelected, out List<Tile> blockers))
            {
                HandleMatch(_firstSelected, _secondSelected);
            }
            else
            {
                foreach (var blocker in blockers)
                    blocker.Shake();

                _firstSelected.Deselect();
                _secondSelected.Deselect();
            }

            _firstSelected = null;
            _secondSelected = null;
        }
        else
        {
            _firstSelected.Deselect();
            _firstSelected = _secondSelected;
            _secondSelected = null;
        }
    }

    public bool IsMatchValid(Tile a, Tile b, out List<Tile> blockingTiles)
    {
        blockingTiles = GetBlockingTiles(a, b);
        return blockingTiles.Count == 0;
    }

    private List<Tile> GetBlockingTiles(Tile a, Tile b)
    {
        var indexA = _boardGenerate.GetTileIndex(a);
        var indexB = _boardGenerate.GetTileIndex(b);

        var result = new List<Tile>();

        var rowA = indexA / Columns;
        var colA = indexA % Columns;
        var rowB = indexB / Columns;
        var colB = indexB % Columns;

        var dRow = rowB - rowA;
        var dCol = colB - colA;

        var steps = Mathf.Max(Mathf.Abs(dRow), Mathf.Abs(dCol));

        // Check if tiles are aligned (same row, column, or diagonal)
        if (steps != 0 && (dRow % steps == 0 && dCol % steps == 0))
        {
            var stepRow = dRow / steps;
            var stepCol = dCol / steps;

            for (int i = 1; i < steps; i++)
            {
                var r = rowA + stepRow * i;
                var c = colA + stepCol * i;
                var index = r * Columns + c;

                var tile = _boardGenerate.GetTileAt(index);
                if (tile != null && tile.IsActive && tile.Value != 0)
                {
                    result.Add(tile);
                }
            }
        }
        else
        {
            var min = Mathf.Min(indexA, indexB);
            var max = Mathf.Max(indexA, indexB);

            for (int i = min + 1; i < max; i++)
            {
                var tile = _boardGenerate.GetTileAt(i);
                if (tile != null && tile.IsActive && tile.Value != 0)
                {
                    result.Add(tile);
                }
            }
        }

        return result;
    }

    private void HandleMatch(Tile a, Tile b)
    {
        if (a.IsGem || b.IsGem)
        {
            foreach (var tile in new[] { a, b })
            {
                if (!tile.IsGem) continue;

                var gemType = tile.Gem;
                EventDispatch.Dispatch(EventDispatchName.GemMatched, gemType);
            }
        }

        a.Match();
        b.Match();
        AudioManager.Instance.PlaySFX("PairClear");
        AddScore(Random.Range(1, 3));

        _rowsToProcess.Clear();

        var indexA = _boardGenerate.GetTileIndex(a);
        var indexB = _boardGenerate.GetTileIndex(b);
        var rowA = indexA / Columns;
        var rowB = indexB / Columns;

        CheckAndMarkRowForClearing(rowA);

        if (rowB != rowA)
        {
            CheckAndMarkRowForClearing(rowB);
        }

        if (_rowsToProcess.Count > 0)
        {
            ProcessClearedRows();
            AudioManager.Instance.PlaySFX("RowClear");
        }

        _boardGenerate.CheckForLoss();
    }

    private void CheckAndMarkRowForClearing(int row)
    {
        var isRowClear = true;

        for (int col = 0; col < Columns; col++)
        {
            int index = row * Columns + col;
            var tile = _boardGenerate.GetTileAt(index);

            if (tile != null && tile.IsActive && tile.Value != 0)
            {
                isRowClear = false;
                break;
            }
        }

        if (isRowClear)
        {
            _rowsToProcess.Add(row);
        }
    }

    private void ProcessClearedRows()
    {
        var sortedRows = new List<int>(_rowsToProcess);
        sortedRows.Sort((a, b) => b.CompareTo(a));

        foreach (int row in sortedRows)
        {
            RemoveRowAndShiftUp(row);
            AddScore(Random.Range(100, 150));
        }

        var allCleared = _boardGenerate.AllTiles.Where(t => t.IsActive).All(t => t.Value == 0);

        if (allCleared)
            _boardGenerate.NextStage();
    }

    private void RemoveRowAndShiftUp(int rowToRemove)
    {
        var totalRows = Mathf.CeilToInt((float)_boardGenerate.AllTiles.Where(t => t.IsActive).Count() / Columns);

        for (int r = rowToRemove; r < totalRows - 1; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                var currentIndex = r * Columns + c;
                var belowIndex = (r + 1) * Columns + c;

                var currentTile = _boardGenerate.GetTileAt(currentIndex);
                var belowTile = _boardGenerate.GetTileAt(belowIndex);

                if (currentTile != null && belowTile != null)
                {
                    currentTile.Clone(belowTile);
                }
            }
        }

        // Clear the last row
        var lastRow = totalRows - 1;
        for (int c = 0; c < Columns; c++)
        {
            var index = lastRow * Columns + c;
            var tile = _boardGenerate.GetTileAt(index);

            if (tile != null)
            {
                tile.Initialize(0, false);
            }
        }
    }

    private void AddScore(int score)
    {
        if (GameManager.Instance.GameMode != GameMode.Normal) return;

        _score += score;
        CheckHighScore();
        GameUIManager.Instance.UpdateScoreText(_score);
    }

    private void CheckHighScore()
    {
        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save();
            GameUIManager.Instance.UpdateHighScoreText(_highScore);
        }
    }
}