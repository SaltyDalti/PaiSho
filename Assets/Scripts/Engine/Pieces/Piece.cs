using UnityEngine;
using PaiSho.Game;
using System.Collections.Generic;

namespace PaiSho.Pieces
{
    public class Piece : MonoBehaviour
    {
        // --- Core Properties ---
        public Player Owner { get; private set; }
        public PieceType Type { get; private set; }

        private int boardCoordinate;
        public int TurnsSinceMoved { get; set; }
        public int TurnsSinceHarmonized { get; set; }
        public int WiltLevel { get; set; }
        public int PreviousWiltLevel { get; set; }
        public int PointValue { get; set; } = 1;
        public bool IsNewThisTurn { get; set; } = true;
        public bool HasMovedThisTurn { get; set; }
        public bool InHarmony { get; set; }
        public bool IsGhost { get; set; } = false;
        public bool FreezeWiltNextTurn { get; set; }

        // --- Initialization ---
        public void Initialize(Player owner, PieceType type)
        {
            Owner = owner;
            Type = type;
        }

        // --- Board Position ---
        public void SetPosition(int coordinate)
        {
            boardCoordinate = coordinate;
        }

        public void SetBoardCoordinate(int coordinate)
        {
            boardCoordinate = coordinate;
        }

        public int GetPosition()
        {
            return boardCoordinate;
        }

        // --- Behavior Flags ---
        public bool IsFlower()
        {
            return Type == PieceType.Jasmine || Type == PieceType.Lily || Type == PieceType.Jade ||
                   Type == PieceType.Rose || Type == PieceType.Rhododendron || Type == PieceType.Chrysanthemum ||
                   Type == PieceType.Lotus || Type == PieceType.Orchid;
        }

        public bool IsNonFlower()
        {
            return Type == PieceType.Boat || Type == PieceType.Rock ||
                   Type == PieceType.Knotweed || Type == PieceType.Wheel;
        }

        public bool IsSpecial()
        {
            return Type == PieceType.Lotus || Type == PieceType.Orchid;
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

        public bool IsImmovable()
        {
            return Type == PieceType.Rock;
        }

        public bool CanMoveOver()
        {
            return Type == PieceType.Orchid;
        }

        // --- Gameplay Logic ---
        public int GetModifiedMovementRange()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();

            if (current == Season.Spring && (Type == PieceType.Jasmine || Type == PieceType.Lily || Type == PieceType.Jade))
                return 2; // +1 movement during spring
            return 1;
        }

        public bool CanBeCaptured()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();

            if (current == Season.Summer && (Type == PieceType.Boat || Type == PieceType.Knotweed))
                return false;
            return true;
        }

        public bool CanFormHarmony()
        {
            return Type != PieceType.Orchid;
        }

        public bool CanFormDisharmony()
        {
            return Type != PieceType.Orchid;
        }

        public bool CanBeDisharmonized()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();

            if (current == Season.Autumn &&
                (Type == PieceType.Rose || Type == PieceType.Chrysanthemum || Type == PieceType.Rhododendron))
                return false;
            return true;
        }

        public bool CanHarmonizeWith(Piece other)
        {
            if (Type == PieceType.Lotus && IsBlooming() && other.IsFlower())
                return true;
            if (other.Type == PieceType.Lotus && other.IsBlooming() && IsFlower())
                return true;

            return Type == other.Type && IsFlower() && other.IsFlower();
        }

        public int GetScoreValue()
        {
            Season current = SeasonManager.Instance.GetCurrentSeason();

            if (current == Season.Winter && (Type == PieceType.Rock || Type == PieceType.Wheel || Type == PieceType.Lotus))
                return PointValue + 1;

            return PointValue;
        }

        public static bool IsFlowerType(PieceType type)
        {
            return type == PieceType.Jasmine ||
                   type == PieceType.Lily ||
                   type == PieceType.Jade ||
                   type == PieceType.Rose ||
                   type == PieceType.Rhododendron ||
                   type == PieceType.Chrysanthemum ||
                   type == PieceType.Lotus ||
                   type == PieceType.Orchid;
        }



        public void SetVisualState(string state)
        {
            Debug.Log($"Piece {Type} changed to visual state: {state}");
            // TODO: Hook this into Unity animations / shaders / materials
        }

        // --- Blooming Logic ---
        public bool IsBlooming()
        {
            if (Type != PieceType.Lotus)
                return false;

            Player opponent = (Owner == Player.Host) ? Player.Opponent : Player.Host;
            return PotManager.Instance.CountCapturedBy(Owner) < PotManager.Instance.CountCapturedBy(opponent);
        }

        // --- Harmony Management ---
        private HashSet<Piece> harmonizedWith = new HashSet<Piece>();

        public bool IsInHarmonyWith(Piece other)
        {
            return harmonizedWith.Contains(other);
        }

        public void AddHarmony(Piece other)
        {
            harmonizedWith.Add(other);
        }

        public void RemoveHarmony(Piece other)
        {
            harmonizedWith.Remove(other);
        }
    }
}
