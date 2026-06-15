using System.Linq;

public class HealerPassive : IPassiveStrategy
{
    // 턴 시작 시 자신을 제외한 아군 필드 카드 HP +1
    public void OnTurnStart(CardInstance owner, BoardManager board)
    {
        var allies = board.GetAliveCards(owner.Team)
                         .Where(c => c != owner);

        foreach (var ally in allies)
            ally.Heal(1);
    }
}
