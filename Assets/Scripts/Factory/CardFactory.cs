using System.Collections.Generic;

public static class CardFactory
{
    public static CardInstance Create(CardData data, Team team)
    {
        var card = new CardInstance(data, team);

        switch (data.cardType)
        {
            case CardType.Infantry:
                card.AttackStrategy  = new NormalAttackStrategy();
                card.SkillStrategy   = new TauntSkill();
                break;
            case CardType.Archer:
                card.AttackStrategy  = new RangedAttackStrategy();
                card.SkillStrategy   = new AimShotSkill();
                break;
            case CardType.Cavalry:
                card.AttackStrategy  = new CavalryAttackStrategy();
                card.SkillStrategy   = new StunSkill();
                break;
            case CardType.Cleric:
                card.AttackStrategy  = new NormalAttackStrategy();
                card.SkillStrategy   = new SingleHealSkill();
                card.PassiveStrategy = new HealerPassive();
                break;
        }

        return card;
    }

    // 보병 2 / 궁수 2 / 기마병 1 / 성직자 1 고정 덱 생성
    public static List<CardInstance> CreateDeck(
        CardData infantry, CardData archer, CardData cavalry, CardData cleric, Team team)
    {
        return new List<CardInstance>
        {
            Create(infantry, team),
            Create(infantry, team),
            Create(archer,   team),
            Create(archer,   team),
            Create(cavalry,  team),
            Create(cleric,   team),
        };
    }
}
