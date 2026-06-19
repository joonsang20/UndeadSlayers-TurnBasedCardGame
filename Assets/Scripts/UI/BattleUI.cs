using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BattleAnimator battleAnimator;
    [SerializeField] private CardView cardViewPrefab;

    [Header("Field Slots")]
    [SerializeField] private Transform[] playerSlots = new Transform[3];
    [SerializeField] private Transform[] enemySlots  = new Transform[3];

    [Header("Reserve")]
    [SerializeField] private GameObject[] playerReserveCards = new GameObject[3];
    [SerializeField] private GameObject[] enemyReserveCards  = new GameObject[3];

    [Header("UI")]
    [SerializeField] private Image playerTurnImage;
    [SerializeField] private Image enemyTurnImage;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private Button btnAttack;
    [SerializeField] private Button btnSkill;
    [SerializeField] private Button btnCancel;
    [SerializeField] private TextMeshProUGUI topExplainText;
    [SerializeField] private TextMeshProUGUI bottomExplainText;

    private readonly CardView[] playerCardViews = new CardView[3];
    private readonly CardView[] enemyCardViews  = new CardView[3];

    private enum InputState { Idle, SelectingTarget }
    private InputState inputState = InputState.Idle;
    private bool isActionPhaseActive = false;

    private CardView selectedActor;
    private ActionType pendingActionType;

    private const float TurnIndicatorDuration = 1.2f;

    private void Start()
    {
        btnAttack.onClick.AddListener(() => OnActionButtonClicked(ActionType.BasicAttack));
        btnSkill.onClick.AddListener(() => OnActionButtonClicked(ActionType.Skill));
        btnCancel.onClick.AddListener(OnCancelClicked);

        gameManager.OnTurnStarted        += HandleTurnStarted;
        gameManager.OnActionPhaseBegin   += HandleActionPhaseBegin;
        gameManager.OnGameOver           += HandleGameOver;
        gameManager.OnActionPerformed    += HandleActionPerformed;
        gameManager.Board.OnCardRefilled += HandleCardRefilled;
        gameManager.BattleResolver.OnCardRemoved += HandleCardRemoved;

        actionPanel.SetActive(false);
        playerTurnImage.gameObject.SetActive(false);
        enemyTurnImage.gameObject.SetActive(false);
        topExplainText.text = "";
        bottomExplainText.text = "";

        gameManager.StartGame();
        SpawnAllCards();
        UpdateReserveDisplay();
    }

    // ───────────── 카드 스폰 ─────────────

    private void SpawnAllCards()
    {
        SpawnTeamCards(Team.Player);
        SpawnTeamCards(Team.Enemy);
    }

    private void SpawnTeamCards(Team team)
    {
        var field = gameManager.Board.GetField(team);
        for (int i = 0; i < 3; i++)
        {
            if (field[i] != null)
                GetViews(team)[i] = CreateCardView(field[i], GetSlots(team)[i], i);
        }
    }

    private CardView CreateCardView(CardInstance card, Transform slot, int index)
    {
        var view = Instantiate(cardViewPrefab, slot);
        view.Setup(card, index);
        view.OnClicked += OnCardViewClicked;
        battleAnimator.Register(card, view);
        return view;
    }

    // ───────────── 턴 처리 ─────────────

    private void HandleTurnStarted(Team team)
    {
        ShowTurnIndicator(team == Team.Player ? playerTurnImage : enemyTurnImage,
                          team == Team.Player ? enemyTurnImage   : playerTurnImage);
        bottomExplainText.text = "";
    }

    private void ShowTurnIndicator(Image show, Image hide)
    {
        hide.gameObject.SetActive(false);

        show.color = new Color(1f, 1f, 1f, 0f);
        show.gameObject.SetActive(true);
        show.DOFade(1f, 0.4f);
    }

    private void HandleActionPhaseBegin(Team team)
    {
        StartCoroutine(ActionPhaseRoutine(team));
    }

    private IEnumerator ActionPhaseRoutine(Team team)
    {
        yield return new WaitForSeconds(0.5f);

        // 패시브 보유 카드가 있으면 연출 재생 후 실제 패시브 실행
        foreach (var card in gameManager.Board.GetField(team))
        {
            if (card == null || !card.IsAlive || card.PassiveStrategy == null) continue;

            var targets = new List<CardInstance>();
            foreach (var ally in gameManager.Board.GetField(team))
                if (ally != null && ally.IsAlive && ally != card)
                    targets.Add(ally);

            if (targets.Count > 0)
                yield return StartCoroutine(battleAnimator.PlayPassiveEffect(card, targets));
        }

        // 애니메이션 완료 후 HP 실제 반영
        gameManager.ExecutePendingPassives();

        yield return new WaitForSeconds(0.7f);

        if (team == Team.Player)
        {
            isActionPhaseActive = true;
            bottomExplainText.text = "Choose a card to use";
            ShowSelectableCards();
        }
        else
        {
            isActionPhaseActive = false;
            bottomExplainText.text = "";
            ClearAllHighlights();
        }
    }

    private void ShowSelectableCards()
    {
        ClearAllHighlights();
        var actable = gameManager.Board.GetActableCards(Team.Player);

        foreach (var view in playerCardViews)
        {
            if (view == null || !view.gameObject.activeSelf) continue;
            if (actable.Contains(view.Card))
                view.SetSelectable();
        }
    }

    // ───────────── 플레이어 입력 처리 ─────────────

    private void OnCardViewClicked(CardView clicked)
    {
        if (gameManager.IsGameOver) return;
        if (gameManager.TurnManager.CurrentTurn != Team.Player) return;
        if (!isActionPhaseActive) return;

        if (inputState == InputState.Idle)
        {
            if (clicked.Card.Team == Team.Player && !clicked.Card.IsStunned)
                SelectActor(clicked);
        }
        else if (inputState == InputState.SelectingTarget)
        {
            if (IsValidTarget(clicked))
                ExecuteAction(clicked);
        }
    }

    private void SelectActor(CardView view)
    {
        ClearAllHighlights();
        selectedActor = view;
        view.SetSelected();
        bottomExplainText.text = "";

        bool skillAvailable = view.Card.IsSkillReady
                              && view.Card.SkillStrategy.CanUse(view.Card, gameManager.Board);
        btnSkill.interactable = skillAvailable;

        inputState = InputState.Idle;
        actionPanel.SetActive(true);
    }

    private void OnActionButtonClicked(ActionType actionType)
    {
        if (selectedActor == null) return;
        pendingActionType = actionType;

        if (actionType == ActionType.Skill && !selectedActor.Card.SkillStrategy.RequiresTarget)
        {
            FinishAction(null);
            return;
        }

        actionPanel.SetActive(false);
        inputState = InputState.SelectingTarget;
        bottomExplainText.text = "Select a target";
        ShowTargetHighlights();
    }

    private void ShowTargetHighlights()
    {
        ClearAllHighlights();
        selectedActor.SetSelected();

        bool isHeal = pendingActionType == ActionType.Skill
                      && selectedActor.Card.SkillStrategy is SingleHealSkill;

        var forced = gameManager.Board.GetForcedTarget(Team.Player);
        var views  = isHeal ? playerCardViews : enemyCardViews;

        foreach (var view in views)
        {
            if (view == null || !view.gameObject.activeSelf) continue;
            if (forced != null && view.Card != forced) continue;
            view.SetAsTarget();
        }
    }

    private bool IsValidTarget(CardView view)
    {
        bool isHeal = pendingActionType == ActionType.Skill
                      && selectedActor.Card.SkillStrategy is SingleHealSkill;

        if (isHeal) return view.Card.Team == Team.Player && view.Card.IsAlive;

        var forced = gameManager.Board.GetForcedTarget(Team.Player);
        if (forced != null) return view.Card == forced;

        return view.Card.Team == Team.Enemy && view.Card.IsAlive;
    }

    private void ExecuteAction(CardView targetView)
    {
        FinishAction(targetView);
    }

    private void FinishAction(CardView targetView)
    {
        ClearAllHighlights();
        actionPanel.SetActive(false);
        inputState = InputState.Idle;
        isActionPhaseActive = false;

        gameManager.OnPlayerAction(selectedActor.Card, pendingActionType, targetView?.Card);
        selectedActor = null;
    }

    private void OnCancelClicked()
    {
        selectedActor = null;
        actionPanel.SetActive(false);
        inputState = InputState.Idle;
        bottomExplainText.text = "Choose a card to use";
        ShowSelectableCards();
    }

    // ───────────── 카드 제거 / 리필 ─────────────

    private void HandleCardRemoved(CardInstance card, Team team)
    {
        UpdateReserveDisplay();
    }

    private void HandleCardRefilled(CardInstance card, Team team, int slotIndex)
    {
        var views = GetViews(team);
        var slots = GetSlots(team);

        views[slotIndex] = CreateCardView(card, slots[slotIndex], slotIndex);
        StartCoroutine(battleAnimator.PlayRefill(views[slotIndex]));
        UpdateReserveDisplay();
    }

    private void UpdateReserveDisplay()
    {
        int playerCount = gameManager.Board.GetReserveCount(Team.Player);
        int enemyCount  = gameManager.Board.GetReserveCount(Team.Enemy);

        for (int i = 0; i < 3; i++)
        {
            if (playerReserveCards[i] != null)
                playerReserveCards[i].SetActive(i < playerCount);
            if (enemyReserveCards[i] != null)
                enemyReserveCards[i].SetActive(i < enemyCount);
        }
    }

    private void HandleActionPerformed(CardInstance actor, ActionType actionType, CardInstance target)
    {
        topExplainText.text = BuildActionMessage(actor, actionType, target);
    }

    private string BuildActionMessage(CardInstance actor, ActionType actionType, CardInstance target)
    {
        string actorName  = actor.Data.cardName;
        string targetName = target?.Data.cardName;

        if (actionType == ActionType.BasicAttack)
            return $"{actorName} attacked {targetName} for {actor.Atk} damage";

        return actor.Data.cardType switch
        {
            CardType.Cleric   => $"{actorName} healed {targetName ?? "allies"}",
            CardType.Infantry => $"{actorName} used Taunt",
            CardType.Archer   => $"{actorName} used Aim Shot on {targetName}",
            CardType.Cavalry  => $"{actorName} used Stun on {targetName}",
            _                 => $"{actorName} used a skill"
        };
    }

    private void HandleGameOver(GameResult result)
    {
        ClearAllHighlights();
        actionPanel.SetActive(false);
        isActionPhaseActive = false;
        topExplainText.text = "";
        bottomExplainText.text = "";
        playerTurnImage.gameObject.SetActive(false);
        enemyTurnImage.gameObject.SetActive(false);
    }

    // ───────────── 유틸 ─────────────

    private void ClearAllHighlights()
    {
        foreach (var v in playerCardViews) v?.ClearHighlight();
        foreach (var v in enemyCardViews)  v?.ClearHighlight();
    }

    private CardView[] GetViews(Team team) =>
        team == Team.Player ? playerCardViews : enemyCardViews;

    private Transform[] GetSlots(Team team) =>
        team == Team.Player ? playerSlots : enemySlots;
}
