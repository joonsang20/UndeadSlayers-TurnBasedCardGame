using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GameResult { None, Win, Lose, Draw }

public class GameManager : MonoBehaviour
{
    [SerializeField] private CardData infantryData;
    [SerializeField] private CardData archerData;
    [SerializeField] private CardData cavalryData;
    [SerializeField] private CardData clericData;

    [SerializeField] private CardData infantryEnemyData;
    [SerializeField] private CardData archerEnemyData;
    [SerializeField] private CardData cavalryEnemyData;
    [SerializeField] private CardData clericEnemyData;

    [SerializeField] private BattleAnimator battleAnimator;

    public BoardManager Board { get; private set; }
    public TurnManager TurnManager { get; private set; }
    public BattleResolver BattleResolver { get; private set; }
    public AIController AIController { get; private set; }

    public GameResult CurrentResult { get; private set; } = GameResult.None;
    public bool IsGameOver => CurrentResult != GameResult.None;

    public event Action<GameResult> OnGameOver;
    public event Action<Team> OnTurnStarted;
    public event Action<Team> OnActionPhaseBegin;

    private readonly List<CardInstance> pendingPassives = new List<CardInstance>();

    private void Awake()
    {
        Board = new BoardManager();
        BattleResolver = new BattleResolver(Board);
        TurnManager = new TurnManager(Board);
        AIController = new AIController(Board, BattleResolver);

        TurnManager.OnTurnStarted += t => OnTurnStarted?.Invoke(t);
        TurnManager.OnActionPhaseBegin += HandleActionPhaseBegin;
        TurnManager.OnPassiveReady += card => pendingPassives.Add(card);
    }

    public void StartGame()
    {
        CurrentResult = GameResult.None;

        var playerDeck = CardFactory.CreateDeck(infantryData, archerData, cavalryData, clericData, Team.Player);
        var enemyDeck  = CardFactory.CreateDeck(infantryEnemyData, archerEnemyData, cavalryEnemyData, clericEnemyData, Team.Enemy);

        Shuffle(playerDeck);
        Shuffle(enemyDeck);

        Board.InitializeField(Team.Player, playerDeck.Take(3).ToList(), playerDeck.Skip(3).ToList());
        Board.InitializeField(Team.Enemy,  enemyDeck.Take(3).ToList(),  enemyDeck.Skip(3).ToList());

        TurnManager.StartFirstTurn();
    }

    public void ExecutePendingPassives()
    {
        foreach (var card in pendingPassives)
            card.PassiveStrategy?.OnTurnStart(card, Board);
        pendingPassives.Clear();
    }

    // UI에서 플레이어 행동 입력 시 호출
    public void OnPlayerAction(CardInstance actor, ActionType actionType, CardInstance target)
    {
        if (IsGameOver || TurnManager.CurrentTurn != Team.Player) return;
        StartCoroutine(ExecutePlayerAction(actor, actionType, target));
    }

    private IEnumerator ExecutePlayerAction(CardInstance actor, ActionType actionType, CardInstance target)
    {
        yield return StartCoroutine(battleAnimator.PlayActionAnimation(actor, actionType, target));

        var snapshot = TakeHpSnapshot();
        BattleResolver.ResolveAction(actor, actionType, target);
        yield return StartCoroutine(PostActionSequence(snapshot));

        if (!CheckGameOver())
            TurnManager.EndTurn();
    }

    private void HandleActionPhaseBegin(Team team)
    {
        OnActionPhaseBegin?.Invoke(team);

        if (team == Team.Enemy && !IsGameOver)
            StartCoroutine(ExecuteEnemyTurn());
    }

    private IEnumerator ExecuteEnemyTurn()
    {
        yield return new WaitForSeconds(0.8f);

        var (actor, actionType, target) = AIController.DecideTurn();
        if (actor != null)
        {
            yield return StartCoroutine(battleAnimator.PlayActionAnimation(actor, actionType, target));

            var snapshot = TakeHpSnapshot();
            BattleResolver.ResolveAction(actor, actionType, target);
            yield return StartCoroutine(PostActionSequence(snapshot));
        }

        yield return null;

        if (!CheckGameOver())
            TurnManager.EndTurn();
    }

    private bool CheckGameOver()
    {
        bool playerHas = Board.HasAnyCards(Team.Player);
        bool enemyHas  = Board.HasAnyCards(Team.Enemy);

        if (!playerHas && !enemyHas)
            CurrentResult = GameResult.Draw;
        else if (!playerHas)
            CurrentResult = GameResult.Lose;
        else if (!enemyHas)
            CurrentResult = GameResult.Win;

        if (!IsGameOver) return false;

        OnGameOver?.Invoke(CurrentResult);
        return true;
    }

    private IEnumerator PostActionSequence(Dictionary<CardInstance, int> snapshot)
    {
        yield return StartCoroutine(battleAnimator.PlayHitsFromDamage(snapshot));
        yield return StartCoroutine(battleAnimator.PlayDeathAnimations(snapshot));
        Board.TryRefill(Team.Player);
        Board.TryRefill(Team.Enemy);
        yield return new WaitForSeconds(0.8f);
    }

    private Dictionary<CardInstance, int> TakeHpSnapshot()
    {
        var snapshot = new Dictionary<CardInstance, int>();
        foreach (var team in new[] { Team.Player, Team.Enemy })
            foreach (var card in Board.GetField(team))
                if (card != null && card.IsAlive)
                    snapshot[card] = card.CurrentHp;
        return snapshot;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
