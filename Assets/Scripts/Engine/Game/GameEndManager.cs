using UnityEngine;

namespace PaiSho.Game
{
    public class GameEndManager : MonoBehaviour
    {
        public static GameEndManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void ResolveFinalScore()
        {
            var scores = ScoringManager.Instance.GetAllScores();
            int hostScore = scores[Player.Host];
            int opponentScore = scores[Player.Opponent];

            Debug.Log($"Final Scores - Host: {hostScore}, Opponent: {opponentScore}");

            if (hostScore > opponentScore)
                Debug.Log("Host wins!");
            else if (opponentScore > hostScore)
                Debug.Log("Opponent wins!");
            else
                Debug.Log("It's a poetic tie!");
        }
    }
}
