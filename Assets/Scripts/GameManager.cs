using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Lab 4 + 6: game state, UI and scene switching.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int targetScore = 20;
    public int lives = 3;
    public Text scoreText;
    public Image progressFill;
    public Image[] heartIcons;
    public GameObject winPanel;
    public GameObject losePanel;
    public Spawner spawner;
    public AudioSource audioSource;
    public AudioClip winClip;
    public AudioClip loseClip;
    public GameObject confettiPrefab;

    public bool IsOver { get; private set; }
    int score;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start() { UpdateUI(); }

    public void AddScore(int points)
    {
        if (IsOver) return;
        score += points;
        UpdateUI();
        if (score >= targetScore) Finish(true);
    }

    public void LoseLife()
    {
        if (IsOver) return;
        lives--;
        UpdateUI();
        if (lives <= 0) Finish(false);
    }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = Mathf.Min(score, targetScore) + " / " + targetScore;
        if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01((float)score / targetScore);
        for (int i = 0; i < heartIcons.Length; i++)
            heartIcons[i].color = i < lives ? Color.white : new Color(1f, 1f, 1f, 0.25f);
    }

    void Finish(bool win)
    {
        IsOver = true;
        if (spawner != null) spawner.enabled = false;
        foreach (FallingItem f in FindObjectsByType<FallingItem>()) Destroy(f.gameObject);
        if (win && confettiPrefab != null) Instantiate(confettiPrefab, new Vector3(0f, 4.5f, 0f), Quaternion.identity);
        if (audioSource != null) audioSource.PlayOneShot(win ? winClip : loseClip);
        (win ? winPanel : losePanel).SetActive(true);
    }

    public void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMenu() { SceneManager.LoadScene("Menu"); }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
