using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BattleAnimator : MonoBehaviour
{
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Transform arrowContainer;

    private readonly Dictionary<CardInstance, CardView> cardViewMap = new();

    public void Register(CardInstance card, CardView view)   => cardViewMap[card] = view;
    public void Unregister(CardInstance card)                => cardViewMap.Remove(card);
    public CardView GetView(CardInstance card)               => cardViewMap.TryGetValue(card, out var v) ? v : null;

    // 행동 전체 애니메이션 진입점
    public IEnumerator PlayActionAnimation(CardInstance actor, ActionType actionType, CardInstance target)
    {
        if (actionType == ActionType.BasicAttack)
            yield return StartCoroutine(PlayAttack(actor, target));
        else
            yield return StartCoroutine(PlaySkill(actor, target));
    }

    // ───────────── 공격 연출 ─────────────

    private IEnumerator PlayAttack(CardInstance actor, CardInstance target)
    {
        switch (actor.Data.cardType)
        {
            case CardType.Archer:
                yield return StartCoroutine(PlayRangedAttack(actor, target));
                break;
            default:
                yield return StartCoroutine(PlayMeleeAttack(actor, target));
                break;
        }
    }

    private IEnumerator PlayMeleeAttack(CardInstance actor, CardInstance target)
    {
        var actorView  = GetView(actor);
        var targetView = GetView(target);
        if (actorView == null || targetView == null) yield break;

        var actorRect  = actorView.GetComponent<RectTransform>();
        Vector3 origin    = actorRect.position;
        Vector3 direction = (targetView.GetComponent<RectTransform>().position - origin).normalized;
        Vector3 movePos   = origin + direction * 80f;

        var seq = DOTween.Sequence();
        seq.Append(actorRect.DOMove(movePos, 0.15f).SetEase(Ease.OutQuad));
        seq.AppendInterval(0.05f);
        seq.Append(actorRect.DOMove(origin, 0.15f).SetEase(Ease.InQuad));
        yield return seq.WaitForCompletion();
    }

    private IEnumerator PlayRangedAttack(CardInstance actor, CardInstance target)
    {
        yield return StartCoroutine(FireArrow(actor, target, new Vector2(80f, 80f), Color.white, 1500f));
    }

    private IEnumerator FireArrow(CardInstance actor, CardInstance target, Vector2 size, Color color, float speed)
    {
        var actorView  = GetView(actor);
        var targetView = GetView(target);
        if (actorView == null || targetView == null || arrowSprite == null) yield break;

        Vector3 startPos = actorView.GetComponent<RectTransform>().position;
        Vector3 endPos   = targetView.GetComponent<RectTransform>().position;

        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(arrowContainer, false);

        var rt = arrowGO.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.position  = startPos;

        var img = arrowGO.AddComponent<Image>();
        img.sprite        = arrowSprite;
        img.color         = color;
        img.raycastTarget = false;

        Vector2 dir = endPos - startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowGO.transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);

        float duration = Mathf.Clamp(Vector3.Distance(startPos, endPos) / speed, 0.15f, 0.5f);
        yield return rt.DOMove(endPos, duration).SetEase(Ease.Linear).WaitForCompletion();

        Destroy(arrowGO);
    }

    // ───────────── 스킬 연출 ─────────────

    private IEnumerator PlaySkill(CardInstance actor, CardInstance target)
    {
        switch (actor.Data.cardType)
        {
            case CardType.Infantry:
                yield return StartCoroutine(PlayTauntEffect(actor));
                break;
            case CardType.Archer:
                yield return StartCoroutine(PlayAimShotEffect(actor, target));
                break;
            case CardType.Cavalry:
                yield return StartCoroutine(PlayStunEffect(actor, target));
                break;
            case CardType.Cleric:
                yield return StartCoroutine(PlayHealEffect(actor, target));
                break;
        }
    }

    private IEnumerator PlayTauntEffect(CardInstance actor)
    {
        var view = GetView(actor);
        if (view != null)
            yield return StartCoroutine(view.PlayTauntAnimation());
    }

    private IEnumerator PlayAimShotEffect(CardInstance actor, CardInstance target)
    {
        // 차징: 액터 스케일 펄스
        var actorView = GetView(actor);
        if (actorView != null)
        {
            var rect = actorView.GetComponent<RectTransform>();
            yield return rect.DOScale(1.12f, 0.2f).SetEase(Ease.OutQuad).WaitForCompletion();
            rect.DOScale(1f, 0.1f);
        }

        // 황금빛 대형 화살 발사
        yield return StartCoroutine(FireArrow(actor, target,
            new Vector2(200f, 200f), new Color(0.2f, 0.7f, 1f), 1700f));
    }

    private IEnumerator PlayStunEffect(CardInstance actor, CardInstance target)
    {
        var view = GetView(target);
        if (view != null)
            yield return StartCoroutine(view.PlayStunAnimation());
    }

    private IEnumerator PlayHealEffect(CardInstance actor, CardInstance target)
    {
        var view = GetView(target);
        if (view != null)
            yield return StartCoroutine(view.PlayHealAnimation());
    }

    // ───────────── 피격 / 사망 / 등장 ─────────────

    public IEnumerator PlayHitsFromDamage(Dictionary<CardInstance, int> beforeHp)
    {
        var damaged = new List<CardInstance>();
        foreach (var kvp in beforeHp)
        {
            if (kvp.Key.CurrentHp < kvp.Value)
                damaged.Add(kvp.Key);
        }

        if (damaged.Count == 0) yield break;

        foreach (var card in damaged)
            StartCoroutine(PlayHit(card));

        yield return new WaitForSeconds(0.35f);
    }

    public IEnumerator PlayHit(CardInstance target)
    {
        var view = GetView(target);
        if (view != null)
            yield return StartCoroutine(view.PlayHit());
    }

    public IEnumerator PlayDeathAnimations(Dictionary<CardInstance, int> snapshot)
    {
        var dead = new List<CardInstance>();
        foreach (var kvp in snapshot)
            if (!kvp.Key.IsAlive)
                dead.Add(kvp.Key);

        if (dead.Count == 0) yield break;

        foreach (var card in dead)
            StartCoroutine(PlayDeath(card));

        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator PlayDeath(CardInstance card)
    {
        var view = GetView(card);
        if (view != null)
        {
            yield return StartCoroutine(view.PlayDeathAnimation());
            Unregister(card);
        }
    }

    public IEnumerator PlayRefill(CardView view)
    {
        yield return StartCoroutine(view.PlaySpawnAnimation());
    }

    public IEnumerator PlayPassiveEffect(CardInstance owner, List<CardInstance> targets)
    {
        foreach (var card in targets)
        {
            var view = GetView(card);
            if (view != null)
                StartCoroutine(view.PlayHealAnimation());
        }
        if (targets.Count > 0)
            yield return new WaitForSeconds(0.55f);
    }
}
