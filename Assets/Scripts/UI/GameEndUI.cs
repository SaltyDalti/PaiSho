using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PaiSho.Game
{
    public class GameEndUI : MonoBehaviour
    {
        public static GameEndUI Instance;

        [Header("End Game UI Elements")]
        public GameObject endGamePanel;
        public TMP_Text victoryText;
        public Button restartButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            if (endGamePanel != null)
                endGamePanel.SetActive(false);
        }

        public void ShowVictory(Player winner)
        {
            if (endGamePanel != null)
                endGamePanel.SetActive(true);

            if (victoryText != null)
            {
                victoryText.text = winner == Player.Host ? "HOST WINS!" : "OPPONENT WINS!";
            }
            else
            {
                Debug.LogError("[GameEndUI] victoryText is not assigned.");
            }

            Time.timeScale = 0f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
