using UnityEngine;
using System.Collections.Generic;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class VictoryManager : MonoBehaviour
    {
        public static VictoryManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool CheckForHarmonyRingEnd(Player currentPlayer, List<Piece> allPieces)
        {
            // Placeholder: Search the board for a "ring"
            Debug.Log("Checking for Harmony Ring...");

            bool ringExists = false; // TODO: Implement ring detection!

            if (ringExists)
            {
                GameManager.Instance.EndGame(currentPlayer);
                return true;
            }

            return false;
        }
    }
}
