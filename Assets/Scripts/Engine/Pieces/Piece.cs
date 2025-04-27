using UnityEngine;
using PaiSho.Game;

namespace PaiSho.Pieces
{
    public partial class Piece : MonoBehaviour
    {
        public int BaseMovementRange = 1;
        public int TurnsSinceMoved = 0;
        public int TurnsSinceHarmonized = 0;
        public int WiltLevel = 0;
        public int PreviousWiltLevel = 0;
        public int PointValue = 1;

        public bool IsNewThisTurn = true;
        public bool HasMovedThisTurn = false;
        public bool InHarmony = false;

        public Player Owner;
        public PieceType Type;

        public void MarkAsMovedThisTurn()
        {
            HasMovedThisTurn = true;
        }

        public bool IsFlower()
        {
            return Type == PieceType.Jasmine || Type == PieceType.Rose || Type == PieceType.Lily ||
                   Type == PieceType.Jade || Type == PieceType.Chrysanthemum || Type == PieceType.Rhododendron;
        }

        public bool IsNonFlower()
        {
            return Type == PieceType.Boat || Type == PieceType.Knotweed || Type == PieceType.Rock || Type == PieceType.Wheel;
        }

        public int GetModifiedMovementRange()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();
            if (current == Season.Spring && (Type == PieceType.Jasmine || Type == PieceType.Lily || Type == PieceType.Jade))
                return BaseMovementRange + 1;

            return BaseMovementRange;
        }

        public bool CanBeCaptured()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();
            if (current == Season.Summer && (Type == PieceType.Boat || Type == PieceType.Knotweed))
                return false;

            return true;
        }

        public bool CanBeDisharmonized()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();
            if (current == Season.Autumn &&
                (Type == PieceType.Rose || Type == PieceType.Chrysanthemum || Type == PieceType.Rhododendron))
                return false;

            return true;
        }

        public int GetScoreValue()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();
            if (current == Season.Winter && (Type == PieceType.Rock || Type == PieceType.Wheel || Type == PieceType.Lotus))
                return PointValue + 1;

            return PointValue;
        }

        public bool IsBlooming()
        {
            if (Type != PieceType.Lotus)
                return false;

            Player opponent = Owner == Player.Host ? Player.Opponent : Player.Host;
            return PotManager.Instance.CountCapturedBy(Owner) < PotManager.Instance.CountCapturedBy(opponent);
        }

        public bool CanHarmonizeWith(Piece other)
        {
            if (Type == PieceType.Lotus && IsBlooming() && other.IsFlower())
                return true;

            if (other.Type == PieceType.Lotus && other.IsBlooming() && IsFlower())
                return true;

            return Type == other.Type && IsFlower() && other.IsFlower();
        }

        public bool CanMoveOver()
        {
            return Type == PieceType.Orchid;
        }

        public bool CanFormHarmony()
        {
            return Type != PieceType.Orchid;
        }

        public bool CanFormDisharmony()
        {
            return Type != PieceType.Orchid;
        }

        public bool IsImmovable()
        {
            return Type == PieceType.Rock;
        }

        public bool CanCarryOthers()
        {
            return Type == PieceType.Boat;
        }

        public bool CausesRotation()
        {
            return Type == PieceType.Wheel;
        }

        public bool BlocksHarmony()
        {
            return Type == PieceType.Knotweed;
        }

        // Add inside your Piece class (bottom is fine)

        // Smoothly update visuals (placeholder for now)
        public void SetVisualState(string state)
        {
            Debug.Log($"SetVisualState called on {Type} with state {state}");
        }

        // Track whether this piece is a Ghost (for Echo Tiles)
        public bool IsGhost { get; set; } = false;

        // Return current "position" (coordinate) - placeholder system
        public int GetPosition()
        {
            // You probably want to tie this to your BoardManager later!
            return transform.GetInstanceID();
        }

        // Whether this Piece is a flower
        public bool IsFlowerType()
        {
            return IsFlower();
        }
    }

}
