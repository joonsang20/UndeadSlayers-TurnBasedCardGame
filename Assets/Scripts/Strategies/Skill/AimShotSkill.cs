using UnityEngine;

public class AimShotSkill : ISkillStrategy
{
    public bool RequiresTarget => true;

    public bool CanUse(CardInstance user, BoardManager board) => true;

    public bool IsValidTarget(CardInstance user, CardInstance target, BoardManager board) =>
        target != null && target.IsAlive && target.Team != user.Team;

    public void Execute(CardInstance user, CardInstance target, BoardManager board)
    {
        int damage = Mathf.CeilToInt(user.Atk * 1.5f);
        target.TakeDamage(damage);
        // 원거리 스킬: 반격 없음
    }
}
