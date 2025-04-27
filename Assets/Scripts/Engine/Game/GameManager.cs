using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private bool gameStarted = false;
        private int currentPlayerIndex = 0;
        private Player[] players = new Player[] { Player.Host, Player.Opponent };
        private bool springPhase = true;
        private bool turnComplete = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            Debug.Log("Spring Opening Phase begins!");
            gameStarted = true;
            springPhase = true;
        }

        public Player GetCurrentPlayer()
        {
            return players[currentPlayerIndex];
        }

        public void MarkTurnComplete()
        {
            turnComplete = true;
        }

        public bool IsSpringPhase()
        {
            return springPhase;
        }

        public PieceType GetOpeningFlower(Player player)
        {
            return player == Player.Host ? PieceType.Jasmine : PieceType.Rose;
        }

        public void EndTurn()
        {
            if (!turnComplete)
            {
                Debug.LogWarning("You must place a tile before ending your turn.");
                return;
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % 2;

            if (GameStateManager.Instance.GetCurrentPhase() == GamePhase.Spring && currentPlayerIndex == 0)
            {
                GameStateManager.Instance.AdvancePhase();
                Debug.Log("Spring Phase complete. Entering normal gameplay.");
            }

            turnComplete = false;

            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();

            // Evaluate tile lifecycle at turn start
            TileLifecycleManager.Instance.OnTurnStart(allPieces);

            // Check for harmonic ring victory condition
            Player current = GetCurrentPlayer();
            bool gameEnded = VictoryManager.Instance.CheckForHarmonyRingEnd(current, allPieces);

            // Award momentum for harmonious play
            MomentumManager.Instance.EvaluateTurnBonuses(current, allPieces);

            if (!gameEnded)
            {
                Debug.Log("Turn ended, no victory condition met.");
            }
        }

        public int GetTurnNumber()
        {
            // Assuming each player takes one turn per "full" turn cycle.
            return currentPlayerIndex + (springPhase ? 0 : 1);
        }

        public void EndGame(Player ringCreator)
        {
            Debug.Log($"Game has ended due to Harmony Ring formed by {ringCreator}.");
            GameEndManager.Instance.ResolveFinalScore();
            GameEndUI.Instance.ShowVictory(ringCreator);
        }
    }
}
