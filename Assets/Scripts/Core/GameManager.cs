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

    public BoardManager Board { get; private set; }
    public TurnManager TurnManager { get; private set; }
    public BattleResolver BattleResolver { get; private set; }
    public AIController AIController { get; private set; }

    public GameResult CurrentResult { get; private set; } = GameResult.None;
    public bool IsGameOver => CurrentResult != GameResult.None;

    public event Action<GameResult> OnGameOver;
    public event Action<Team> OnTurnStarted;
    public event Action<Team> OnActionPhaseBegin;

    private void Awake()
    {
        Board = new BoardManager();
        BattleResolver = new BattleResolver(Board);
        TurnManager = new TurnManager(Board);
        AIController = new AIController(Board, BattleResolver);

        TurnManager.OnTurnStarted += t => OnTurnStarted?.Invoke(t);
        TurnManager.OnActionPhaseBegin += HandleActionPhaseBegin;
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

    // UI에서 플레이어 행동 입력 시 호출
    public void OnPlayerAction(CardInstance actor, ActionType actionType, CardInstance target)
    {
        if (IsGameOver || TurnManager.CurrentTurn != Team.Player) return;
        StartCoroutine(ExecutePlayerAction(actor, actionType, target));
    }

    private IEnumerator ExecutePlayerAction(CardInstance actor, ActionType actionType, CardInstance target)
    {
        BattleResolver.ResolveAction(actor, actionType, target);

        yield return null; // 연출 대기 슬롯 (추후 DOTween 코루틴으로 교체)

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

        AIController.ExecuteTurn();

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

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
