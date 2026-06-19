using UnityEngine;

public enum CardType { Infantry, Archer, Cavalry, Cleric }

[CreateAssetMenu(fileName = "CardData", menuName = "UndeadSlayers/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int maxHp;
    public int atk;
    public int skillCooldown;
    public Sprite illustration;
    [TextArea(2, 4)]
    public string skillDescription;
}
