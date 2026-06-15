public class NormalAttackStrategy : IAttackStrategy
{
    public void Execute(CardInstance attacker, CardInstance target, BoardManager board)
    {
        target.TakeDamage(attacker.Atk);
        if (target.IsAlive)
            attacker.TakeDamage(target.Atk);
    }
}
