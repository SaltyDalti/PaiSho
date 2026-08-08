using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Minimal ferry: a boat may carry one friendly flower, move with it, then unload adjacent.
    /// Push rules remain in LegalMoveCalculator / TileSelector.
    /// </summary>
    public class BoatManager : MonoBehaviour
    {
        public static BoatManager Instance;

        private const string CargoBadgeName = "CargoBadge";

        private readonly Dictionary<Piece, Piece> cargoByBoat = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void ClearAll()
        {
            foreach (var pair in cargoByBoat)
            {
                if (pair.Value != null)
                    pair.Value.transform.SetParent(null, true);
            }

            cargoByBoat.Clear();
        }

        public void ClearCargoForBoat(Piece boat)
        {
            if (boat == null)
                return;

            RemoveCargoBadge(boat);

            if (!cargoByBoat.TryGetValue(boat, out Piece cargo) || cargo == null)
            {
                cargoByBoat.Remove(boat);
                return;
            }

            cargoByBoat.Remove(boat);
            if (cargo.transform.parent == boat.transform)
                cargo.transform.SetParent(BoardManager.Instance != null
                    ? BoardManager.Instance.transform
                    : null, true);
        }

        public Piece GetCargo(Piece boat)
        {
            if (boat == null)
                return null;
            return cargoByBoat.TryGetValue(boat, out Piece cargo) ? cargo : null;
        }

        public bool HasCargo(Piece boat) => GetCargo(boat) != null;

        public bool CanLoad(Piece boat, Piece passenger)
        {
            if (boat == null || passenger == null)
                return false;
            if (boat.Type != PieceType.Boat || !boat.CanCarryOthers())
                return false;
            if (HasCargo(boat))
                return false;
            if (passenger.Owner != boat.Owner)
                return false;
            if (!passenger.IsFlower() || passenger.IsGhost)
                return false;
            if (passenger.IsImmovable())
                return false;
            if (passenger.BoardCoordinate < 0 || boat.BoardCoordinate < 0)
                return false;

            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(boat.BoardCoordinate))
            {
                if (neighbor == passenger.BoardCoordinate)
                    return true;
            }

            return false;
        }

        public bool TryLoad(Piece boat, Piece passenger)
        {
            if (!CanLoad(boat, passenger))
                return false;

            BoardManager.Instance.LiftPiece(passenger);
            cargoByBoat[boat] = passenger;
            passenger.transform.SetParent(boat.transform, true);
            SeatCargoOnBoat(boat, passenger);
            PieceStateAnimator.Ensure(passenger)?.SyncFromPiece(immediate: true);
            AddCargoBadge(boat);
            DebugLogger.Log($">>> {boat.Owner}'s Boat loaded {passenger.Type}.");
            GameplayFeedback.Show($"{passenger.Type} boarded the Boat.");
            return true;
        }

        public bool CanUnload(Piece boat, int coordinate)
        {
            Piece cargo = GetCargo(boat);
            if (cargo == null || boat == null)
                return false;
            if (!BoardUtils.IsValidPointCoordinate(coordinate))
                return false;
            if (BoardUtils.IsPort(coordinate))
                return false;
            if (BoardManager.Instance.GetPieceAt(coordinate) != null)
                return false;
            bool adjacent = false;
            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(boat.BoardCoordinate))
            {
                if (neighbor == coordinate)
                {
                    adjacent = true;
                    break;
                }
            }

            if (!adjacent)
                return false;
            if (!PieceRules.IsValidBasicFlowerLanding(cargo.Type, coordinate) &&
                PieceRules.IsBasicFlower(cargo.Type))
                return false;

            return true;
        }

        public bool TryUnload(Piece boat, int coordinate)
        {
            if (!CanUnload(boat, coordinate))
                return false;

            Piece cargo = GetCargo(boat);
            cargoByBoat.Remove(boat);
            RemoveCargoBadge(boat);
            cargo.transform.SetParent(
                BoardManager.Instance != null ? BoardManager.Instance.transform : null,
                true);
            BoardManager.Instance.PlacePiece(coordinate, cargo);
            PieceStateAnimator.Ensure(cargo)?.RefreshAfterBoardSeat();
            DebugLogger.Log($">>> Boat unloaded {cargo.Type} at {coordinate}.");
            GameplayFeedback.Show($"{cargo.Type} left the Boat.");
            return true;
        }

        /// <summary>Small floating glyph over a loaded Boat — makes cargo state readable at a glance.</summary>
        private static void AddCargoBadge(Piece boat)
        {
            if (boat == null || boat.transform.Find(CargoBadgeName) != null)
                return;

            float spacing = BoardManager.Instance?.GetBoardLayout()?.CellSpacing ?? 1f;
            Vector3 badgePosition = boat.transform.position + Vector3.up * spacing * 0.6f;
            GameObject badge = WoodTheme.CreateGlowDisc(badgePosition, spacing * 0.16f, JapaneseTheme.MomentumMarker);
            badge.name = CargoBadgeName;
            badge.transform.SetParent(boat.transform, true);

            var animator = badge.AddComponent<OverlayAnimator>();
            animator.Configure(OverlayAnimator.Style.GemPulse, badge.transform, 2.2f, 0.08f);
        }

        private static void RemoveCargoBadge(Piece boat)
        {
            if (boat == null)
                return;

            Transform badge = boat.transform.Find(CargoBadgeName);
            if (badge != null)
                Object.Destroy(badge.gameObject);
        }

        public void OnBoatSeated(Piece boat)
        {
            Piece cargo = GetCargo(boat);
            if (cargo != null)
                SeatCargoOnBoat(boat, cargo);
        }

        private static void SeatCargoOnBoat(Piece boat, Piece cargo)
        {
            if (boat == null || cargo == null)
                return;

            float lift = BoardManager.Instance?.GetBoardLayout()?.CellSpacing * 0.72f ?? 0.3f;
            cargo.transform.localPosition = new Vector3(0f, lift, 0f);
            cargo.transform.localRotation = Quaternion.Euler(0f, cargo.BoardYawDegrees, 0f);
            cargo.transform.localScale = Vector3.one * 0.88f;
        }
    }
}
