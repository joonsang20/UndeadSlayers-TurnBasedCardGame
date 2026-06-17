using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnQuit;

    private void Start()
    {
        btnStart.onClick.AddListener(OnStartClicked);
        btnQuit.onClick.AddListener(OnQuitClicked);
    }

    private void OnStartClicked()
    {
        ScreenFader.Load("BattleScene");
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }
}
