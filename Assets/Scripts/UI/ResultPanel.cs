using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button btnRestart;

    private void Start()
    {
        gameManager.OnGameOver += HandleGameOver;
        btnRestart.onClick.AddListener(OnRestartClicked);
        panel.SetActive(false);
    }

    private void HandleGameOver(GameResult result)
    {
        resultText.text = result switch
        {
            GameResult.Win  => "You Win!!",
            GameResult.Lose => "You Lose...",
            GameResult.Draw => "Draw",
            _               => string.Empty
        };

        resultText.color = result switch
        {
            GameResult.Win  => Color.blue,
            GameResult.Lose => Color.red,
            GameResult.Draw => Color.green,
            _               => Color.white
        };

        panel.SetActive(true);
    }

    private void OnRestartClicked()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
