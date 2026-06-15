public class RangedAttackStrategy : IAttackStrategy
{
    public void Execute(CardInstance attacker, CardInstance target, BoardManager board)
    {
        target.TakeDamage(attacker.Atk);
        // 원거리: 반격 없음
    }
}
