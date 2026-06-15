public class StunSkill : ISkillStrategy
{
    public bool RequiresTarget => true;

    public bool CanUse(CardInstance user, BoardManager board) => true;

    // 이미 기절 중인 카드도 타겟 가능 (지속 시간 연장)
    public bool IsValidTarget(CardInstance user, CardInstance target, BoardManager board) =>
        target != null && target.IsAlive && target.Team != user.Team;

    public void Execute(CardInstance user, CardInstance target, BoardManager board)
    {
        target.ApplyStun();
    }
}
