using UnityEngine;
using UnityEngine.UI;

namespace PaiSho.Game
{
    public class GameEndUI : MonoBehaviour
    {
        public static GameEndUI Instance;

        [Header("End Game UI Elements")]
        public GameObject endGamePanel;
        public Text victoryText;
        public Button restartButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            endGamePanel.SetActive(false); // Hide by default
        }

        public void ShowVictory(Player winner)
        {
            endGamePanel.SetActive(true);

            if (winner == Player.Host)
                victoryText.text = "HOST WINS!";
            else
                victoryText.text = "OPPONENT WINS!";

            Time.timeScale = 0f; // Pause the game
        }

        public void RestartGame()
        {
            Time.timeScale = 1f; // Unpause
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
