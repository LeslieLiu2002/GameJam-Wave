using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject gameWinMenu;
    [SerializeField] private int rewardsToWin = 2;
    public Image m_ref;

    private bool isPaused;
    private bool isGameOver;
    private bool isGameWin;
    private int collectedRewards;
    private PlayerInputHub pih;

    private bool HasEnded => isGameOver || isGameWin;

    void Start()
    {
        pih = GameObject.Find("GameController").GetComponent<PlayerInputHub>();
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        if (gameWinMenu != null) gameWinMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false);
        collectedRewards = 0;
    }

    void Update()
    {
        if (HasEnded || pih == null) return;

        if (pih.Settings)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (HasEnded) return;

        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (HasEnded) return;

        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (settingsMenu != null) settingsMenu.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (settingsMenu != null) settingsMenu.SetActive(false);
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isPaused = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (settingsMenu != null) settingsMenu.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(true);
    }

    public void GameWin()
    {
        if (isGameWin) return;

        isGameWin = true;
        isPaused = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (settingsMenu != null) settingsMenu.SetActive(false);
        if (gameWinMenu != null) gameWinMenu.SetActive(true);
    }

    public void OnRewardCollected()
    {
        if (HasEnded) return;

        collectedRewards++;
        // 更新收集值UI
        if (m_ref == null) return;
        float ratio = Mathf.Clamp01((float)collectedRewards / Mathf.Max(1, rewardsToWin));
        m_ref.fillAmount = ratio;

        if (collectedRewards >= rewardsToWin)
        {
            GameWin();
        }
    }

    public void OnResumeClicked()
    {
        if (!HasEnded) Resume();
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
