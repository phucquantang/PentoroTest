using System.Collections.Generic;
using Common;
using TMPro;
using DG.Tweening; // Add this for DOTween
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;
    public bool IsAddingNumbers;

    public int CurrentAddNumberCount
    {
        get => _currentAddNumberCount;
        set
        {
            _currentAddNumberCount = value;
            _hintText.text = value.ToString();
        }
    }

    private int _currentAddNumberCount;

    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _gameWinPanel;
    [SerializeField] private TMP_Text _hintText;
    [SerializeField] private TMP_Text[] _gemsText;
    [SerializeField] private BoardGenerator _boardGenerator;
    [SerializeField] private GameObject _overlay;
    [SerializeField] private TMP_Text _stageText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _highScoreText;

    private void OnEnable()
    {
        EventDispatch.AddListener(gameObject, OnGameStart, EventDispatchName.GameStart);
        EventDispatch.AddListener(gameObject, OnGemMatched, EventDispatchName.GemMatched);
        EventDispatch.AddListener(gameObject, OnWin, EventDispatchName.Win);
        EventDispatch.AddListener(gameObject, OnLose, EventDispatchName.Lose);
    }

    private void OnDisable()
    {
        EventDispatch.RemoveListener(OnGameStart, EventDispatchName.GameStart);
        EventDispatch.RemoveListener(OnGemMatched, EventDispatchName.GemMatched);
        EventDispatch.RemoveListener(OnWin, EventDispatchName.Win);
        EventDispatch.RemoveListener(OnLose, EventDispatchName.Lose);
    }
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
    }

    private void OnGameStart(object param)
    {
        if (GameManager.Instance.GameMode == GameMode.Gem)
        {
            var gemTargets = GameManager.Instance.GemTargets;

            for (int i = 0; i < _gemsText.Length; i++)
            {
                var type = (GemType)i;
                var _gem = _gemsText[i];

                if (gemTargets.ContainsKey(type))
                {
                    _gem.transform.parent.gameObject.SetActive(true);
                    _gem.text = gemTargets[type].ToString();
                }
                else
                {
                    _gem.transform.parent.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            _gemsText[0].transform.parent.parent.gameObject.SetActive(false);
            _scoreText.transform.parent.gameObject.SetActive(true);
            _highScoreText.transform.parent.gameObject.SetActive(true);
            _scoreText.text = "0";
            UpdateHighScoreText(PlayerPrefs.GetInt("HighScore", 0));
        }

        _stageText.text = "Stage: 1";
        _gameOverPanel.SetActive(false);
        _gameWinPanel.SetActive(false);
        IsAddingNumbers = false;
        CurrentAddNumberCount = GameManager.Instance.AddNumberCount;
    }

    private void OnGemMatched(object param)
    {
        if (param is not GemType type) return;

        var gemTargets = GameManager.Instance.GemTargets;
        switch (type)
        {
            case GemType.Gem1:
                {
                    _gemsText[0].text = gemTargets[type].ToString();
                    break;
                }
            case GemType.Gem2:
                {
                    _gemsText[1].text = gemTargets[type].ToString();
                    break;
                }
            case GemType.Gem3:
                {
                    _gemsText[2].text = gemTargets[type].ToString();
                    break;
                }
        }
    }

    public void OnLose(object param)
    {
        AnimatePanel(_gameOverPanel, true);
    }

    public void OnWin(object param)
    {
        AnimatePanel(_gameWinPanel, true);
    }

    public void ReturnToMainMenu()
    {
        LoadingManager.Instance.LoadScene("MainMenu");
        PlayPopSound();
    }

    public void NewGame()
    {
        AnimatePanel(_gameWinPanel, false);
        GameManager.Instance.NewGame();
        PlayPopSound();
    }

    public void RestartGame()
    {
        AnimatePanel(_gameOverPanel, false);
        GameManager.Instance.NewGame();
        PlayPopSound();
    }

    public void UseAddNumbers()
    {
        if (CurrentAddNumberCount <= 0 || IsAddingNumbers) return;

        IsAddingNumbers = true;
        _boardGenerator.AppendActiveTiles();
        CurrentAddNumberCount--;
        PlayPopSound();
    }

    public void UpdateStageText(int stage)
    {
        _stageText.text = $"Stage {stage}";
    }

    public void UpdateScoreText(int score)
    {
        _scoreText.text = score.ToString();
    }

    public void UpdateHighScoreText(int highScore)
    {
        _highScoreText.text = highScore.ToString();
    }

    public void UseShowPair()
    {
        var (tile1, tile2) = _boardGenerator.HasAnyPair();
        if (tile1 != null && tile2 != null)
        {
            tile1.ShowPair();
            tile2.ShowPair();
        }
    }

    private void AnimatePanel(GameObject panel, bool show)
    {
        _overlay.SetActive(show);

        if (show)
        {
            panel.SetActive(true);
            panel.transform.localScale = Vector3.zero;
            panel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
        else
        {
            panel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => panel.SetActive(false));
        }
    }

    private void PlayPopSound()
    {
        AudioManager.Instance.PlaySFX("Pop");
    }
}
