public interface IAttackStrategy
{
    void Execute(CardInstance attacker, CardInstance target, BoardManager board);
}
