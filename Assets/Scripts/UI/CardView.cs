using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image border;
    [SerializeField] private TextMeshProUGUI hpText;

    private CanvasGroup canvasGroup;
    private Tween provokePulseTween;
    private bool isProvokingActive;

    private static readonly Color ColorSelectable  = new Color(0f, 1f, 0f, 1f);
    private static readonly Color ColorSelected    = new Color(0f, 1f, 0f, 1f);
    private static readonly Color ColorTarget      = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color ColorProvoke     = new Color(1f, 0.85f, 0.1f, 1f);
    private static readonly Color ColorStun        = new Color(0.65f, 0.2f, 1f, 1f);

    public CardInstance Card { get; private set; }
    public int SlotIndex { get; private set; }

    public event Action<CardView> OnClicked;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(CardInstance card, int slotIndex)
    {
        Card = card;
        SlotIndex = slotIndex;

        card.OnHpChanged      += UpdateHp;
        card.OnDied           += HandleDied;
        card.OnStunChanged    += HandleStunChanged;
        card.OnProvokeChanged += HandleProvokeChanged;

        if (card.Data.illustration != null)
            cardImage.sprite = card.Data.illustration;

        UpdateHp(card.CurrentHp, card.MaxHp);
        SetBorderActive(false);
    }

    // ───────────── 하이라이트 ─────────────

    public void SetSelectable()
    {
        provokePulseTween?.Kill();
        SetBorderActive(true);
        border.color = ColorSelectable;
    }

    public void SetSelected()
    {
        provokePulseTween?.Kill();
        SetBorderActive(true);
        border.color = ColorSelected;
    }

    public void SetAsTarget()
    {
        provokePulseTween?.Kill();
        SetBorderActive(true);
        border.color = ColorTarget;
    }

    public void ClearHighlight()
    {
        if (isProvokingActive)
            StartProvokePulse();
        else
        {
            provokePulseTween?.Kill();
            SetBorderActive(false);
        }
    }

    // ───────────── 애니메이션 ─────────────

    public IEnumerator PlayHit()
    {
        var rect = GetComponent<RectTransform>();
        var seq = DOTween.Sequence();
        seq.Append(cardImage.DOColor(new Color(1f, 0.3f, 0.3f), 0.05f));
        seq.Append(cardImage.DOColor(Color.white, 0.2f));
        seq.Join(rect.DOShakeAnchorPos(0.3f, 8f, 20, 90, false));
        yield return seq.WaitForCompletion();

        // 기절 상태면 보라색 복원
        if (Card != null && Card.IsStunned)
            cardImage.color = ColorStun;
    }

    public IEnumerator PlayTauntAnimation()
    {
        var rect = GetComponent<RectTransform>();
        var seq = DOTween.Sequence();
        seq.Append(rect.DOScale(1.18f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(cardImage.DOColor(ColorProvoke, 0.15f));
        seq.Append(rect.DOScale(1f, 0.25f).SetEase(Ease.InQuad));
        seq.Join(cardImage.DOColor(Color.white, 0.25f));
        yield return seq.WaitForCompletion();
    }

    public IEnumerator PlayStunAnimation()
    {
        var rect = GetComponent<RectTransform>();
        var seq = DOTween.Sequence();
        seq.Append(cardImage.DOColor(ColorStun, 0.08f));
        seq.Append(cardImage.DOColor(Color.white, 0.35f));
        seq.Join(rect.DOShakeAnchorPos(0.35f, 10f, 22, 90, false));
        yield return seq.WaitForCompletion();
    }

    public IEnumerator PlayHealAnimation()
    {
        var seq = DOTween.Sequence();
        seq.Append(cardImage.DOColor(new Color(0.2f, 1f, 0.35f), 0.12f));
        seq.Append(cardImage.DOColor(Color.white, 0.4f));
        yield return seq.WaitForCompletion();

        if (Card != null && Card.IsStunned)
            cardImage.color = ColorStun;
    }

    public IEnumerator PlaySpawnAnimation()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.4f);
        yield return new WaitForSeconds(0.4f);
    }

    public IEnumerator PlayDeathAnimation()
    {
        provokePulseTween?.Kill();
        isProvokingActive = false;
        SetBorderActive(false);
        canvasGroup.DOFade(0f, 0.4f);
        yield return new WaitForSeconds(0.45f);
        gameObject.SetActive(false);
        canvasGroup.alpha = 1f;
    }

    // ───────────── 상태 변화 ─────────────

    private void HandleDied()
    {
        ClearHighlight();
    }

    private void HandleStunChanged(bool isStunned)
    {
        cardImage.DOKill();
        cardImage.color = isStunned ? ColorStun : Color.white;
    }

    private void HandleProvokeChanged(bool isProvoking)
    {
        isProvokingActive = isProvoking;
        if (isProvoking)
            StartProvokePulse();
        else
        {
            provokePulseTween?.Kill();
            provokePulseTween = null;
            SetBorderActive(false);
            border.color = Color.white;
        }
    }

    private void StartProvokePulse()
    {
        provokePulseTween?.Kill();
        border.color = ColorProvoke;
        SetBorderActive(true);
        provokePulseTween = border.DOFade(0.3f, 0.7f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    // ───────────── 유틸 ─────────────

    private void UpdateHp(int current, int max)
    {
        hpText.text = $"{current}/{max}";
    }

    private void SetBorderActive(bool active)
    {
        if (border != null)
            border.gameObject.SetActive(active);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Card != null && Card.Team == Team.Player)
            TooltipUI.Instance?.Show(Card);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        provokePulseTween?.Kill();
        if (Card == null) return;
        Card.OnHpChanged      -= UpdateHp;
        Card.OnDied           -= HandleDied;
        Card.OnStunChanged    -= HandleStunChanged;
        Card.OnProvokeChanged -= HandleProvokeChanged;
    }
}
