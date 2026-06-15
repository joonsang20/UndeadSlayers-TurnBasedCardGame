using System;
using System.Collections.Generic;

public enum ActionType { BasicAttack, Skill }

public class BattleResolver
{
    private readonly BoardManager board;

    public event Action<CardInstance, Team> OnCardRemoved;

    public BattleResolver(BoardManager board)
    {
        this.board = board;
    }

    public void ResolveAction(CardInstance actor, ActionType actionType, CardInstance target)
    {
        if (actionType == ActionType.BasicAttack)
        {
            actor.AttackStrategy.Execute(actor, target, board);
        }
        else
        {
            actor.SkillStrategy.Execute(actor, target, board);
            actor.ResetCooldown();
        }

        ProcessDeaths();
    }

    private void ProcessDeaths()
    {
        var deadCards = new List<(CardInstance card, Team team)>();

        foreach (Team team in new[] { Team.Player, Team.Enemy })
        {
            foreach (var card in board.GetField(team))
            {
                if (card != null && !card.IsAlive)
                    deadCards.Add((card, team));
            }
        }

        if (deadCards.Count == 0) return;

        // 판정 순서: 도발 해제 → 기절 해제(생략, 죽으면 무의미) → 필드 제거
        foreach (var (card, team) in deadCards)
        {
            card.ClearProvoke();
            board.RemoveCard(card, team);
            OnCardRemoved?.Invoke(card, team);
        }

        // 양측 동시 리필
        board.TryRefill(Team.Player);
        board.TryRefill(Team.Enemy);
    }
}
