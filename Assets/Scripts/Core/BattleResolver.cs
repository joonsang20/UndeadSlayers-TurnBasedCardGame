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

        foreach (var (card, team) in deadCards)
        {
            card.ClearProvoke();
            board.RemoveCard(card, team);
            OnCardRemoved?.Invoke(card, team);
        }

        board.TryRefill(Team.Player);
        board.TryRefill(Team.Enemy);
    }

}
