using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class GameLogManager : MonoBehaviour
    {
        public static GameLogManager Instance;

        private List<string> logEntries = new ();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void LogMove(Player player, PieceType type, int from, int to)
        {
            string entry = $"{player} moved {type} from {from} to {to} (Turn {GameManager.Instance.GetTurnNumber()})";
            logEntries.Add(entry);
            Debug.Log(entry);
        }

        public void LogPlacement(Player player, PieceType type, int position)
        {
            string entry = $"{player} placed {type} at {position} (Turn {GameManager.Instance.GetTurnNumber()})";
            logEntries.Add(entry);
            Debug.Log(entry);
        }

        public List<string> GetLogEntries()
        {
            return new List<string>(logEntries);
        }
    }
}
