public class SingleHealSkill : ISkillStrategy
{
    private const int HealAmount = 5;

    public bool RequiresTarget => true;

    public bool CanUse(CardInstance user, BoardManager board) => true;

    // 자신 포함 아군 전체 힐 가능
    public bool IsValidTarget(CardInstance user, CardInstance target, BoardManager board) =>
        target != null && target.IsAlive && target.Team == user.Team;

    public void Execute(CardInstance user, CardInstance target, BoardManager board)
    {
        target.Heal(HealAmount);
    }
}
