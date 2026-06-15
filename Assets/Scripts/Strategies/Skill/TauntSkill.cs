public class TauntSkill : ISkillStrategy
{
    public bool RequiresTarget => false;

    public bool CanUse(CardInstance user, BoardManager board)
    {
        // 같은 팀에 이미 활성 도발이 있으면 사용 불가
        var provoker = board.ActiveProvoker;
        if (provoker == null) return true;
        if (provoker.Team == user.Team && provoker != user) return false;
        return true;
    }

    public bool IsValidTarget(CardInstance user, CardInstance target, BoardManager board) => true;

    public void Execute(CardInstance user, CardInstance target, BoardManager board)
    {
        board.SetProvoke(user);
    }
}
