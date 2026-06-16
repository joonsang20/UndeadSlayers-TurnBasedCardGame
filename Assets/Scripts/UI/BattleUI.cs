using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CardView cardViewPrefab;

    [Header("Field Slots")]
    [SerializeField] private Transform[] playerSlots = new Transform[3];
    [SerializeField] private Transform[] enemySlots  = new Transform[3];

    [Header("Reserve")]
    [SerializeField] private GameObject[] playerReserveCards = new GameObject[3];
    [SerializeField] private GameObject[] enemyReserveCards  = new GameObject[3];

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private Button btnAttack;
    [SerializeField] private Button btnSkill;
    [SerializeField] private Button btnCancel;

    private readonly CardView[] playerCardViews = new CardView[3];
    private readonly CardView[] enemyCardViews  = new CardView[3];

    private enum InputState { Idle, SelectingTarget }
    private InputState inputState = InputState.Idle;

    private CardView selectedActor;
    private ActionType pendingActionType;

    private void Start()
    {
        btnAttack.onClick.AddListener(() => OnActionButtonClicked(ActionType.BasicAttack));
        btnSkill.onClick.AddListener(() => OnActionButtonClicked(ActionType.Skill));
        btnCancel.onClick.AddListener(OnCancelClicked);

        gameManager.OnTurnStarted        += HandleTurnStarted;
        gameManager.OnActionPhaseBegin   += HandleActionPhaseBegin;
        gameManager.OnGameOver           += HandleGameOver;
        gameManager.Board.OnCardRefilled += HandleCardRefilled;
        gameManager.BattleResolver.OnCardRemoved += HandleCardRemoved;

        actionPanel.SetActive(false);

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
        return view;
    }

    // ───────────── 턴 처리 ─────────────

    private void HandleTurnStarted(Team team)
    {
        turnIndicatorText.text = team == Team.Player ? "Player Turn" : "Enemy Turn";
    }

    private void HandleActionPhaseBegin(Team team)
    {
        if (team == Team.Player)
            ShowSelectableCards();
        else
            ClearAllHighlights();
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

        // 타겟 불필요 스킬 (도발) 즉시 실행
        if (actionType == ActionType.Skill && !selectedActor.Card.SkillStrategy.RequiresTarget)
        {
            FinishAction(null);
            return;
        }

        actionPanel.SetActive(false);
        inputState = InputState.SelectingTarget;
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

        gameManager.OnPlayerAction(selectedActor.Card, pendingActionType, targetView?.Card);
        selectedActor = null;
    }

    private void OnCancelClicked()
    {
        selectedActor = null;
        actionPanel.SetActive(false);
        inputState = InputState.Idle;
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

    private void HandleGameOver(GameResult result)
    {
        ClearAllHighlights();
        actionPanel.SetActive(false);
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
