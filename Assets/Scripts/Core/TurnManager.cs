using System;

public class TurnManager
{
    private readonly BoardManager board;

    public Team CurrentTurn { get; private set; }

    public event Action<Team> OnTurnStarted;
    public event Action<Team> OnActionPhaseBegin;
    public event Action<Team> OnTurnEnded;

    public TurnManager(BoardManager board)
    {
        this.board = board;
    }

    public void StartFirstTurn()
    {
        CurrentTurn = Team.Player;
        ProcessTurnStart();
    }

    private void ProcessTurnStart()
    {
        var field = board.GetField(CurrentTurn);

        // 1. 쿨타임 감소 (기절 카드 포함)
        foreach (var card in field)
        {
            if (card != null && card.IsAlive)
                card.ReduceCooldown();
        }

        // 2. 기절 해제 (이전 턴 종료에서 카운터가 0이 된 카드)
        foreach (var card in field)
        {
            if (card != null && card.IsAlive)
                card.TryReleaseStun();
        }

        // 3. 패시브 발동 (기절 중인 힐러도 패시브 작동)
        foreach (var card in field)
        {
            if (card != null && card.IsAlive)
                card.PassiveStrategy?.OnTurnStart(card, board);
        }

        OnTurnStarted?.Invoke(CurrentTurn);
        OnActionPhaseBegin?.Invoke(CurrentTurn);
    }

    public void EndTurn()
    {
        Team ending = CurrentTurn;
        Team opposing = ending == Team.Player ? Team.Enemy : Team.Player;

        // 현재 팀의 기절 카운터 감소 (이번 턴 행동 못 한 기절 카드들)
        foreach (var card in board.GetField(ending))
        {
            if (card != null && card.IsAlive && card.IsStunned)
                card.TickStun();
        }

        // 상대 팀의 도발 카운터 감소 (이번 턴 도발 강제 타겟이 소비됨)
        foreach (var card in board.GetField(opposing))
        {
            if (card != null && card.IsProvoking)
                card.TickProvoke();
        }

        // TickProvoke에서 ClearProvoke를 호출하므로 ActiveProvoker 동기화
        if (board.ActiveProvoker != null && !board.ActiveProvoker.IsProvoking)
            board.ClearActiveProvoker();

        OnTurnEnded?.Invoke(ending);

        CurrentTurn = opposing;
        ProcessTurnStart();
    }
}
