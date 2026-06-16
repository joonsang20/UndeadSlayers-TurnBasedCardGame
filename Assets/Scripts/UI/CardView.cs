using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image border;
    [SerializeField] private TextMeshProUGUI hpText;

    public CardInstance Card { get; private set; }
    public int SlotIndex { get; private set; }

    public event Action<CardView> OnClicked;

    private static readonly Color ColorSelectable = new Color(0f, 1f, 0f, 1f);   // 초록 (선택 가능)
    private static readonly Color ColorSelected   = new Color(1f, 0.9f, 0f, 1f); // 노랑 (선택됨)
    private static readonly Color ColorTarget     = new Color(1f, 0.3f, 0.3f, 1f); // 빨강 (타겟 가능)

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

    public void SetSelectable()
    {
        SetBorderActive(true);
        border.color = ColorSelectable;
    }

    public void SetSelected()
    {
        SetBorderActive(true);
        border.color = ColorSelected;
    }

    public void SetAsTarget()
    {
        SetBorderActive(true);
        border.color = ColorTarget;
    }

    public void ClearHighlight()
    {
        SetBorderActive(false);
    }

    private void UpdateHp(int current, int max)
    {
        hpText.text = $"{current}/{max}";
    }

    private void HandleDied()
    {
        ClearHighlight();
        gameObject.SetActive(false); // DOTween 연출 추가 전 임시 처리
    }

    private void HandleStunChanged(bool isStunned)
    {
        // 추후 아이콘 처리
    }

    private void HandleProvokeChanged(bool isProvoking)
    {
        // 추후 아이콘 처리
    }

    private void SetBorderActive(bool active)
    {
        if (border != null)
            border.gameObject.SetActive(active);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (Card == null) return;
        Card.OnHpChanged      -= UpdateHp;
        Card.OnDied           -= HandleDied;
        Card.OnStunChanged    -= HandleStunChanged;
        Card.OnProvokeChanged -= HandleProvokeChanged;
    }
}
