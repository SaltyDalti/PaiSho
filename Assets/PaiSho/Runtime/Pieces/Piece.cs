using UnityEngine;
using PaiSho.Board;
using PaiSho.Game;
using PaiSho;

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
        public bool FreezeWiltNextTurn = false;
        public bool HasMovedSincePlaced = false;

        /// <summary>True while adjacent to an enemy Knotweed — harmony broken and cannot move.</summary>
        public bool IsDrained = false;

        public Player Owner;
        public PieceType Type;
        public int BoardCoordinate { get; private set; } = -1;

        public bool UsesPrefabVisual { get; set; }

        /// <summary>Local Y rotation applied when the tile rests on the board.</summary>
        public float BoardYawDegrees { get; private set; }

        public void AssignRandomBoardYaw()
        {
            BoardYawDegrees = Random.Range(0f, 360f);
        }

        public void SetBoardYawDegrees(float degrees)
        {
            BoardYawDegrees = degrees;
        }

        public void Configure(Player owner, PieceType type, int coordinate, bool seatOnBoard = true)
        {
            Owner = owner;
            Type = type;
            HasMovedSincePlaced = false;
            SetCoordinate(coordinate);

            float cellSpacing = BoardManager.Instance?.GetBoardLayout()?.CellSpacing ?? 0.42f;

            if (UsesPrefabVisual)
            {
                if (BoardCoordinate >= 0 && seatOnBoard)
                    WoodTheme.SeatOnWoodSurface(gameObject);
                // Hand / pot tiles are seated by their tray/pot layout — do not recenter here.

                WoodTheme.EnsurePiecePickCollider(gameObject, cellSpacing);
                PieceMaterialUtility.ApplyPieceTheme(gameObject, Type, Owner);
                if (BoardCoordinate >= 0)
                    PieceStateAnimator.Ensure(this)?.RefreshAfterBoardSeat();
                return;
            }

            WoodTheme.PreparePlacedTile(gameObject, type, owner, cellSpacing);
            WoodTheme.EnsurePiecePickCollider(gameObject, cellSpacing);
            if (BoardCoordinate >= 0)
                PieceStateAnimator.Ensure(this)?.RefreshAfterBoardSeat();
        }

        public void SetCoordinate(int coordinate)
        {
            BoardCoordinate = coordinate;
        }

        public int GetPosition() => BoardCoordinate;

        public void MarkAsMovedThisTurn()
        {
            HasMovedThisTurn = true;
            HasMovedSincePlaced = true;
        }

        public bool CanContributeToHarmony()
        {
            if (!PieceRules.IsBasicFlower(Type) && !PieceRules.IsSpecialFlower(Type))
                return true;

            if (GameManager.Instance != null && GameManager.Instance.IsGlobalHarmonyUnlocked())
                return true;

            return HasMovedSincePlaced;
        }

        public static bool IsFlowerType(PieceType type)
        {
            return type == PieceType.Jasmine || type == PieceType.Rose || type == PieceType.Lily ||
                   type == PieceType.Jade || type == PieceType.Chrysanthemum || type == PieceType.Rhododendron;
        }

        public bool IsFlower() => IsFlowerType(Type);

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
            if (BoardUtils.IsGate(BoardCoordinate))
                return false;

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
            // Blooms for the player who has lost the most pieces (handicap).
            return PotManager.Instance.CountCapturedBy(Owner) > PotManager.Instance.CountCapturedBy(opponent);
        }

        public bool CanHarmonizeWith(Piece other)
        {
            if (other == null || Owner != other.Owner)
                return false;

            if (Type == PieceType.Lotus && IsBlooming() && other.IsFlower())
                return true;

            if (other.Type == PieceType.Lotus && other.IsBlooming() && IsFlower())
                return true;

            if (!IsFlower() || !other.IsFlower())
                return false;

            return PieceHarmonyProfiles.Get(Type).Harmonic.Contains(other.Type);
        }

        public bool CanMoveOver() => Type == PieceType.Orchid;
        public bool CanFormHarmony() => Type != PieceType.Orchid;
        public bool CanFormDisharmony() => Type != PieceType.Orchid;
        public bool IsImmovable() =>
            Type == PieceType.Rock || Type == PieceType.Knotweed || IsDrained;
        public bool CanCarryOthers() => Type == PieceType.Boat;
        public bool CausesRotation() => Type == PieceType.Wheel;
        public bool BlocksHarmony() => Type == PieceType.Knotweed;

        public void SetVisualState(string state)
        {
            var animator = PieceStateAnimator.Ensure(this);

            // Prefab tiles: flower state is driven only by PieceStateAnimator (emission MPB).
            if (UsesPrefabVisual)
            {
                animator?.SyncFromPiece(immediate: true);
                return;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                string partName = renderer.gameObject.name;

                if (!WoodTheme.IsFlowerRenderer(partName))
                {
                    if (partName == "OwnerBand")
                    {
                        WoodTheme.ApplyFlowerColor(renderer, WoodTheme.GetOwnerWood(Owner));
                        continue;
                    }

                    if (partName is "WoodBody" or "WoodRim")
                    {
                        Color tint = WoodTheme.GetOwnerBodyColor(Owner, Type);
                        if (state == "ghost")
                            tint = Color.Lerp(tint, new Color(0.7f, 0.85f, 1f), 0.45f);
                        WoodTheme.ApplyWood(renderer, tint, WoodTheme.GetOwnerBodySmoothness(Owner));
                    }

                    continue;
                }

                Color flower = WoodTheme.GetFlowerAccent(Type);
                if (state is "wilted" or "fully-wilted")
                    flower = Color.Lerp(flower, new Color(0.55f, 0.42f, 0.32f), state == "fully-wilted" ? 0.7f : 0.45f);
                else if (state == "ghost")
                    flower = Color.Lerp(flower, new Color(0.75f, 0.85f, 1f), 0.5f);
                else if (state == "bud")
                    flower = Color.Lerp(flower, new Color(1f, 0.78f, 0.86f), 0.4f);

                WoodTheme.ApplyFlowerColor(renderer, flower);
            }

            animator?.SyncFromPiece(immediate: true);
        }

        public bool IsGhost { get; set; } = false;
    }
}
