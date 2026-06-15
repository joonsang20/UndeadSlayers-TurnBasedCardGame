public interface ISkillStrategy
{
    bool RequiresTarget { get; }
    bool CanUse(CardInstance user, BoardManager board);
    bool IsValidTarget(CardInstance user, CardInstance target, BoardManager board);
    void Execute(CardInstance user, CardInstance target, BoardManager board);
}
