using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private int currentPlayerIndex = 0;
        private Player[] players = new Player[] { Player.Host, Player.Opponent };
        private bool springPhase = true;
        private bool turnComplete = false;
        private int turnNumber = 0;

        private bool hostSpringPlaced = false;
        private bool opponentSpringPlaced = false;


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
            springPhase = true;
            turnNumber = 0;
            PieceSelectionUI.Instance?.HidePanel();
        }

        public Player GetCurrentPlayer()
        {
            return players[currentPlayerIndex];
        }

        public void MarkTurnComplete()
        {
            turnComplete = true;

            if (springPhase)
            {
                Player player = GetCurrentPlayer();
                if (player == Player.Host)
                    hostSpringPlaced = true;
                else if (player == Player.Opponent)
                    opponentSpringPlaced = true;
            }
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

            if (springPhase)
            {
                if (hostSpringPlaced && opponentSpringPlaced)
                {
                    GameStateManager.Instance.AdvancePhase();
                    springPhase = false;
                    Debug.Log("Spring Phase complete. Entering normal gameplay.");

                    PieceSelectionUI.Instance?.ShowPanel();
                }
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % 2;
            turnNumber++;
            turnComplete = false;

            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();

            if (!springPhase)
            {
                // Lifecycle must run before ClearTurnData so HasMovedThisTurn is still valid.
                TileLifecycleManager.Instance.OnTurnStart(allPieces);
                SeasonManager.Instance?.AdvanceTurn();
                SeasonManager.Instance?.EvaluateSeasonalBonuses(GetCurrentPlayer(), allPieces);
            }

            MovementManager.Instance.ClearTurnData();

            Player current = GetCurrentPlayer();
            bool gameEnded = VictoryManager.Instance.CheckForHarmonyRingEnd(current, allPieces);

            MomentumManager.Instance.EvaluateTurnBonuses(current, allPieces);

            if (!gameEnded)
            {
                Debug.Log($"Turn {turnNumber} ended ({current}'s turn next).");
            }
        }


        public int GetTurnNumber()
        {
            return turnNumber;
        }

        public void EndGame(Player ringCreator)
        {
            Debug.Log($"Game has ended due to Harmony Ring formed by {ringCreator}.");
            GameEndManager.Instance.ResolveFinalScore();
            GameEndUI.Instance.ShowVictory(ringCreator);
        }
    }
}
