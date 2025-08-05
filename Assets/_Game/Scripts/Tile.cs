using Common;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public bool IsGem { get; private set; }
    public GemType Gem;

    [SerializeField] private int _value;
    [SerializeField] private Image _clickEffect;
    [SerializeField] private Image _appearEffect;
    [SerializeField] private Image _image;
    [SerializeField] private Sprite _gemClickEffectSprite;
    [SerializeField] private Sprite _tileDefaultSprite;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private Button _button;

    private bool _isClicked;

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            _valueText.text = value.ToString();
        }
    }

    private void Awake()
    {
        _value = 0;
    }

    public void OnClick()
    {
        ToggleSelection();
        EventDispatch.Dispatch(EventDispatchName.TileClicked, this);
    }

    public void ToggleSelection()
    {
        if (_isClicked)
            Deselect();
        else
            Select();
    }

    public void Initialize(int value, bool isActive)
    {
        IsActive = isActive;

        if (!isActive)
        {
            InitializeEmpty();
            return;
        }

        Value = value;
        _valueText.enabled = true;
        _button.interactable = true;
        _valueText.color = new Color(_valueText.color.r, _valueText.color.g, _valueText.color.b, 1f);
        _isClicked = false;
        _clickEffect.gameObject.SetActive(false);
        PlayAppearEffect();
    }

    public void Shake()
    {
        _valueText.transform
            .DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20)
            .SetEase(Ease.OutQuad);
    }

    public void SetAsGem(Sprite gemImage, GemType gemType)
    {
        _image.sprite = gemImage;
        Gem = gemType;
        _clickEffect.sprite = _gemClickEffectSprite;
        IsGem = true;
    }

    public void Match()
    {
        _value = 0;

        _clickEffect.DOKill(true);
        _clickEffect.transform.localScale = Vector3.one;
        _clickEffect.transform.DOScale(Vector3.zero, 0.3f);
        _clickEffect.gameObject.SetActive(false);

        if (IsGem)
        {
            _image.sprite = _tileDefaultSprite;
            _clickEffect.sprite = null;
            IsGem = false;
        }

        GrayOut();
        _isClicked = false;
    }

    private void PlayAppearEffect()
    {
        _appearEffect.gameObject.SetActive(true);
        _appearEffect.transform.localScale = Vector3.one;

        _appearEffect.transform
            .DOScale(Vector3.zero, 0.4f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => _appearEffect.gameObject.SetActive(false));
    }

    public void Deselect()
    {
        _isClicked = false;
        _clickEffect.DOKill(true);

        _clickEffect
            .DOFade(0f, 0.1f)
            .OnComplete(() =>
            {
                _clickEffect.gameObject.SetActive(false);
                _clickEffect.transform.localScale = Vector3.zero;
            });
    }

    public void Clone(Tile originalTile)
    {
        Value = originalTile.Value;
        IsActive = originalTile.IsActive;
        _valueText.enabled = originalTile._valueText.enabled;
        _valueText.text = originalTile._valueText.text;
        _valueText.color = originalTile._valueText.color;
        _image.sprite = originalTile._image.sprite;
        IsGem = originalTile.IsGem;
        Gem = originalTile.Gem;
        _button.interactable = originalTile._button.interactable;
        _clickEffect.gameObject.SetActive(false);
        _appearEffect.gameObject.SetActive(false);
    }

    private void Select()
    {
        _isClicked = true;
        _clickEffect.DOKill(true);

        _clickEffect.gameObject.SetActive(true);
        _clickEffect.color = new Color(_clickEffect.color.r, _clickEffect.color.g, _clickEffect.color.b, 1f);
        _clickEffect.transform.localScale = Vector3.zero;

        _clickEffect.transform
            .DOScale(Vector3.one, 0.2f)
            .SetEase(Ease.OutBack);

        AudioManager.Instance.PlaySFX("ChooseNumber");
    }

    private void InitializeEmpty()
    {
        Value = -1;
        _valueText.text = string.Empty;
        _valueText.enabled = false;
        _clickEffect.gameObject.SetActive(false);
        _image.sprite = _tileDefaultSprite;
        _appearEffect.gameObject.SetActive(false);
        _button.interactable = false;
        _isClicked = false;
    }

    private void GrayOut()
    {
        _valueText.color = new Color(_valueText.color.r, _valueText.color.g, _valueText.color.b, 0.5f);
        _button.interactable = false;
    }
}

