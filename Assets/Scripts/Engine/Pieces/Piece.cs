using UnityEngine;
using PaiSho.Game;
using PaiSho.Domain;
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
        public bool IsFlower() => PieceTraits.IsFlower(Type);

        public bool IsNonFlower() => PieceTraits.IsAccent(Type);

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

        public bool BlocksHarmony() => PieceTraits.BlocksHarmony(Type);

        public bool IsImmovable()
        {
            return Type == PieceType.Rock;
        }

        public bool CanMoveOver()
        {
            return Type == PieceType.Orchid;
        }

        // --- Gameplay Logic ---
        public int GetModifiedMovementRange() =>
            SeasonRules.ModifiedMovementRange(Type, SeasonMapping.Current());

        public bool CanBeCaptured() =>
            CaptureRules.CanBeCaptured(Type, SeasonMapping.Current());

        public bool CanFormHarmony() => PieceTraits.CanFormHarmony(Type);

        public bool CanFormDisharmony() => PieceTraits.CanFormHarmony(Type);

        public bool CanBeDisharmonized() =>
            CaptureRules.CanBeDisharmonized(Type, SeasonMapping.Current());

        public bool CanHarmonizeWith(Piece other)
        {
            if (other == null)
                return false;

            return HarmonyRules.CanHarmonizeTypes(
                Type,
                other.Type,
                IsBlooming(),
                other.IsBlooming());
        }

        public int GetScoreValue() =>
            SeasonRules.BaseScoreValue(Type, SeasonMapping.Current(), PointValue);

        public static bool IsFlowerType(PieceType type) => PieceTraits.IsFlower(type);



        public void SetVisualState(string state)
        {
            Debug.Log($"Piece {Type} changed to visual state: {state}");
            ApplyVisualTint(state);
        }

        private void ApplyVisualTint(string state)
        {
            Color tint = state switch
            {
                "ghost" => new Color(0.65f, 0.8f, 1f, 0.55f),
                "wilted" => new Color(0.55f, 0.45f, 0.25f, 1f),
                "fully-wilted" => new Color(0.35f, 0.28f, 0.18f, 1f),
                "blooming" => new Color(1f, 0.75f, 0.9f, 1f),
                "vibrant" => Color.white,
                _ => Color.white
            };

            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer == null || renderer.material == null)
                    continue;
                // Preserve albedo roughly; multiply for wilt/bloom feedback.
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    Color baseColor = renderer.material.GetColor("_BaseColor");
                    renderer.material.SetColor("_BaseColor", baseColor * tint);
                }
                else if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color *= tint;
                }
            }
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
            InHarmony = harmonizedWith.Count > 0;
        }

        public void RemoveHarmony(Piece other)
        {
            harmonizedWith.Remove(other);
            InHarmony = harmonizedWith.Count > 0;
        }
    }
}
