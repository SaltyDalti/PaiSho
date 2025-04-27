using UnityEngine;
using PaiSho.Game;
using PaiSho.Board;
using PaiSho.Pieces;
using System.Collections.Generic;

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
            Debug.Log("Resolving final scores...");

            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();

            int hostScore = ScoringManager.Instance.CalculateScore(Player.Host, allPieces);
            int opponentScore = ScoringManager.Instance.CalculateScore(Player.Opponent, allPieces);

            Debug.Log($"Host Final Score: {hostScore}");
            Debug.Log($"Opponent Final Score: {opponentScore}");

            if (hostScore > opponentScore)
                Debug.Log("Host wins!");
            else if (opponentScore > hostScore)
                Debug.Log("Opponent wins!");
            else
                Debug.Log("The game ends in a draw!");

            // Optional: Trigger end screen or transition back to main menu
        }
    }
}
