using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Lab 4 + 6: loading screen, scene switching, buttons.
public class MenuController : MonoBehaviour
{
    public GameObject loadingOverlay;
    public Text loadingText;
    public GameObject howToPanel;

    public void Play() { StartCoroutine(LoadGame()); }
    public void ShowHowTo(bool show) { howToPanel.SetActive(show); }
    public void ShowHowToOn() { howToPanel.SetActive(true); }
    public void ShowHowToOff() { howToPanel.SetActive(false); }

    IEnumerator LoadGame()
    {
        loadingOverlay.SetActive(true);
        AsyncOperation op = SceneManager.LoadSceneAsync("Game");
        op.allowSceneActivation = false;
        float t = 0f;
        while (op.progress < 0.9f || t < 1.4f)
        {
            t += Time.deltaTime;
            loadingText.text = "Loading" + new string('.', (int)(t * 3f) % 4);
            yield return null;
        }
        op.allowSceneActivation = true;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
