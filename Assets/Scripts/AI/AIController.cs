using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIController
{
    private readonly BoardManager board;
    private readonly BattleResolver resolver;

    public AIController(BoardManager board, BattleResolver resolver)
    {
        this.board = board;
        this.resolver = resolver;
    }

    public void ExecuteTurn()
    {
        var actable = board.GetActableCards(Team.Enemy);
        if (actable.Count == 0) return;

        var actor = actable[Random.Range(0, actable.Count)];
        var actionType = DetermineAction(actor);
        var target = SelectTarget(actor, actionType);

        if (target == null) return;

        resolver.ResolveAction(actor, actionType, target);
    }

    private ActionType DetermineAction(CardInstance actor)
    {
        if (!actor.IsSkillReady) return ActionType.BasicAttack;

        // 리치: 필드 아군 전체 HP가 최대치면 평타
        if (actor.SkillStrategy is SingleHealSkill)
        {
            bool allAtMax = board.GetAliveCards(Team.Enemy).All(c => c.CurrentHp == c.MaxHp);
            if (allAtMax) return ActionType.BasicAttack;
        }

        // 스킬 유효 타겟이 없으면 평타로 fallback
        if (actor.SkillStrategy.RequiresTarget && GetValidSkillTargets(actor).Count == 0)
            return ActionType.BasicAttack;

        return ActionType.Skill;
    }

    private CardInstance SelectTarget(CardInstance actor, ActionType actionType)
    {
        // 힐 스킬: 아군 중 현재 HP 최저 카드
        if (actionType == ActionType.Skill && actor.SkillStrategy is SingleHealSkill)
            return board.GetAliveCards(Team.Enemy).OrderBy(c => c.CurrentHp).FirstOrDefault();

        // 공격: 도발 강제 타겟 우선, 없으면 무작위
        var forced = board.GetForcedTarget(Team.Enemy);
        if (forced != null) return forced;

        var targets = board.GetAliveCards(Team.Player);
        return targets.Count > 0 ? targets[Random.Range(0, targets.Count)] : null;
    }

    private List<CardInstance> GetValidSkillTargets(CardInstance actor)
    {
        if (actor.SkillStrategy is SingleHealSkill)
            return board.GetAliveCards(Team.Enemy);

        return board.GetAliveCards(Team.Player);
    }
}
