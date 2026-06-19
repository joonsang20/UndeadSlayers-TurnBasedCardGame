using TMPro;
using UnityEngine;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI contentText;

    private RectTransform rect;
    private Canvas canvas;

    private void Awake()
    {
        Instance = this;
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        rect.pivot = new Vector2(0f, 1f);
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>())
            graphic.raycastTarget = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        Vector2 pos = Input.mousePosition;
        float tooltipWidth = rect.rect.width * canvas.scaleFactor;

        if (pos.x + tooltipWidth + 15f > Screen.width)
        {
            rect.pivot = new Vector2(1f, 0f);
            pos.x -= 15f;
        }
        else
        {
            rect.pivot = new Vector2(0f, 0f);
            pos.x += 15f;
        }

        pos.y += 15f;
        rect.position = pos;
    }

    public void Show(CardInstance card)
    {
        string attack = BuildAttackLine(card);
        string skill = string.IsNullOrEmpty(card.Data.skillDescription)
            ? ""
            : $"Skill: {card.Data.skillDescription}";

        contentText.text = string.IsNullOrEmpty(skill) ? attack : $"{attack}\n\n{skill}";
        gameObject.SetActive(true);
    }

    private string BuildAttackLine(CardInstance card)
    {
        return card.Data.cardType switch
        {
            CardType.Infantry => $"Attack: Deals {card.Atk} damage to target.\nTakes counter damage in return.",
            CardType.Cleric   => $"Attack: Deals {card.Atk} damage to target.\nTakes counter damage in return.",
            CardType.Cavalry  => $"Attack: Deals {card.Atk} damage to target.\nAlso deals 50% splash damage to adjacent enemies.",
            _                 => $"Attack: Deals {card.Atk} damage to target."
        };
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
