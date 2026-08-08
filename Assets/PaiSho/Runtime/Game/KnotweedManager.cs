using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class KnotweedManager : MonoBehaviour
    {
        public static KnotweedManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Enemy flowers adjacent to Knotweed are drained: harmony breaks and they cannot move
        /// until the Knotweed is gone (or they are no longer adjacent).
        /// </summary>
        public void ProcessDrainEffects()
        {
            if (BoardManager.Instance == null)
                return;

            var shouldDrain = new HashSet<Piece>();
            var drainOwners = new Dictionary<Piece, Player>();

            foreach (Piece knotweed in BoardManager.Instance.GetAllPieces())
            {
                if (knotweed == null || knotweed.Type != PieceType.Knotweed)
                    continue;

                foreach (int neighborCoord in BoardUtils.GetAdjacentCoordinates(knotweed.BoardCoordinate))
                {
                    Piece neighbor = BoardManager.Instance.GetPieceAt(neighborCoord);
                    if (neighbor == null || neighbor.Owner == knotweed.Owner)
                        continue;
                    if (!neighbor.IsFlower())
                        continue;

                    if (shouldDrain.Add(neighbor))
                        drainOwners[neighbor] = knotweed.Owner;
                }
            }

            bool changed = false;
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null)
                    continue;

                bool drain = shouldDrain.Contains(piece);
                if (drain == piece.IsDrained)
                {
                    if (drain)
                        piece.InHarmony = false;
                    continue;
                }

                changed = true;
                if (drain)
                {
                    piece.IsDrained = true;
                    piece.InHarmony = false;
                    PieceStateAnimator.Ensure(piece)?.NotifyDrain();
                    if (drainOwners.TryGetValue(piece, out Player knotweedOwner))
                        TileLifecycleManager.Instance?.RegisterKnotweedDrain(knotweedOwner);
                    DebugLogger.Log($"Knotweed drained {piece.Type} — cannot move while adjacent.");
                }
                else
                {
                    piece.IsDrained = false;
                    DebugLogger.Log($"{piece.Type} is no longer drained.");
                }
            }

            if (changed)
            {
                BoardManager.Instance.RefreshAllHarmony();
                GameplayVisualizer.Instance?.Refresh();
            }
        }
    }
}
