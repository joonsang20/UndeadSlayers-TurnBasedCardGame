using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FadeIn();
    }

    public void FadeIn(float duration = 0.5f)
    {
        fadeImage.raycastTarget = false;
        fadeImage.DOFade(0f, duration);
    }

    public void FadeToScene(string sceneName, float duration = 0.5f)
    {
        fadeImage.raycastTarget = true;
        fadeImage.DOFade(1f, duration).OnComplete(() =>
            SceneManager.LoadScene(sceneName));
    }

    public static void Load(string sceneName, float duration = 0.5f)
    {
        if (Instance != null)
            Instance.FadeToScene(sceneName, duration);
        else
            SceneManager.LoadScene(sceneName);
    }
}
