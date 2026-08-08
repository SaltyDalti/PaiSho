using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PlacementValidator : MonoBehaviour
    {
        public static PlacementValidator Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool CanPlace(Player player, PieceType type, int coordinate)
        {
            if (!BoardUtils.IsValidPointCoordinate(coordinate))
                return false;

            Piece occupant = BoardManager.Instance.GetPieceAt(coordinate);
            if (occupant != null)
                return false;

            if (!ReserveManager.Instance.HasAvailableToPlace(player, type))
                return false;

            if (MovementManager.Instance != null &&
                (MovementManager.Instance.PlacedThisTurn(player) || MovementManager.Instance.GetMovedTileCount(player) > 0))
                return false;

            if (GameStateManager.Instance.IsSpringPhase())
                return CanPlaceDuringSpring(player, type, coordinate);

            if (PieceRules.IsBasicFlower(type))
                return CanPlaceFlowerDuringPlay(player, type, coordinate);

            if (PieceRules.IsNonFlower(type))
                return CanPlaceNonFlower(type, coordinate);

            if (PieceRules.IsSpecialFlower(type))
                return CanPlaceSpecialFlower(player, type, coordinate);

            return false;
        }

        public bool CanPlacePiece(Player player, PieceType type, int coordinate)
        {
            return CanPlace(player, type, coordinate);
        }

        public List<int> GetLegalPlacements(Player player, PieceType type)
        {
            return LegalMoveCalculator.GetLegalPlacements(player, type);
        }

        private bool CanPlaceDuringSpring(Player player, PieceType type, int coordinate)
        {
            if (!PieceRules.IsBasicFlower(type))
                return false;

            if (!IsOnOwnSide(coordinate, player))
                return false;

            return PieceRules.IsValidSpringFlowerGarden(type, coordinate);
        }

        private bool CanPlaceFlowerDuringPlay(Player player, PieceType type, int coordinate)
        {
            return PieceRules.IsValidPortEntry(player, type, coordinate);
        }

        private bool CanPlaceNonFlower(PieceType type, int coordinate)
        {
            return type switch
            {
                PieceType.Knotweed => PieceRules.IsAwayFromPorts(coordinate),
                PieceType.Wheel => PieceRules.IsNeutralGarden(coordinate),
                PieceType.Boat => PieceRules.IsColoredGarden(coordinate),
                PieceType.Rock => true,
                _ => false
            };
        }

        private bool CanPlaceSpecialFlower(Player player, PieceType type, int coordinate)
        {
            if (!GameManager.Instance.SpecialTilesUnlocked(player))
                return false;

            if (type == PieceType.Lotus)
                return IsOnOwnSide(coordinate, player);

            if (type == PieceType.Orchid)
                return IsOnOpponentSide(coordinate, player);

            return false;
        }

        public bool IsOnOwnSide(int coordinate, Player player)
        {
            return player == Player.Host
                ? BoardUtils.IsHostSide(coordinate)
                : BoardUtils.IsOpponentSide(coordinate);
        }

        public bool IsOnOpponentSide(int coordinate, Player player)
        {
            return player == Player.Host
                ? BoardUtils.IsOpponentSide(coordinate)
                : BoardUtils.IsHostSide(coordinate);
        }

        public bool CanMoveTo(Piece piece, int coordinate)
        {
            return TryGetLegalMove(piece, coordinate, out _);
        }

        public bool TryGetLegalMove(Piece piece, int coordinate, out LegalMove legalMove)
        {
            legalMove = default;

            if (piece == null || piece.IsImmovable())
                return false;

            if (MovementManager.Instance != null && MovementManager.Instance.PlacedThisTurn(piece.Owner))
                return false;

            foreach (LegalMove move in GetLegalMoves(piece))
            {
                if (move.Coordinate != coordinate)
                    continue;

                legalMove = move;
                return true;
            }

            return false;
        }

        public List<LegalMove> GetLegalMoves(Piece piece)
        {
            if (piece == null || piece.IsImmovable())
                return new List<LegalMove>();

            if (MovementManager.Instance != null)
            {
                if (!MovementManager.Instance.CanMoveTile(piece))
                    return new List<LegalMove>();

                if (MovementManager.Instance.PlacedThisTurn(piece.Owner))
                    return new List<LegalMove>();
            }

            return LegalMoveCalculator.GetLegalMoves(piece);
        }

        public string ExplainPlacementFailure(Player player, PieceType type, int coordinate)
        {
            if (!BoardUtils.IsValidPointCoordinate(coordinate))
                return "That point isn't on the board.";

            Piece occupant = BoardManager.Instance.GetPieceAt(coordinate);

            if (occupant != null)
                return "That space is already occupied.";

            if (!ReserveManager.Instance.HasAvailableToPlace(player, type))
                return "You don't have that tile in hand or reserve.";

            if (MovementManager.Instance != null &&
                (MovementManager.Instance.PlacedThisTurn(player) || MovementManager.Instance.GetMovedTileCount(player) > 0))
                return "You already placed or moved a tile this turn.";

            if (GameStateManager.Instance.IsSpringPhase())
                return ExplainSpringPlacementFailure(player, type, coordinate);

            if (PieceRules.IsBasicFlower(type))
                return ExplainFlowerPlacementFailure(player, type, coordinate);

            if (PieceRules.IsNonFlower(type))
                return ExplainNonFlowerPlacementFailure(type, coordinate);

            if (PieceRules.IsSpecialFlower(type))
                return ExplainSpecialFlowerPlacementFailure(player, type, coordinate);

            return "That tile can't be placed here.";
        }

        public string ExplainMoveFailure(Piece piece, int coordinate)
        {
            if (piece == null)
                return "No tile selected.";

            if (piece.IsImmovable())
                return $"{piece.Type} can't be moved.";

            if (MovementManager.Instance != null && MovementManager.Instance.PlacedThisTurn(piece.Owner))
                return "You placed a tile this turn — you can't also move.";

            if (MovementManager.Instance != null && !MovementManager.Instance.CanMoveTile(piece))
                return "You've already moved your tile this turn (spend momentum for another move).";

            if (!BoardUtils.IsValidPointCoordinate(coordinate))
                return "That point isn't on the board.";

            if (BoardUtils.IsPort(coordinate))
                return "Tiles can't move onto a port.";

            if (TryGetLegalMove(piece, coordinate, out _))
                return "That move should be legal — try again.";

            Piece target = BoardManager.Instance.GetPieceAt(coordinate);
            if (target != null && target.Owner == piece.Owner)
                return "You can't move onto your own tile.";

            if (target != null)
                return $"{piece.Type} can't capture {target.Type} there.";

            if (LegalMoveCalculator.IsReachableIgnoringDisharmony(piece, coordinate))
            {
                if (LegalMoveCalculator.IsDestinationInDisharmony(piece, coordinate))
                {
                    Piece blocker = LegalMoveCalculator.GetDisharmonyBlocker(piece, coordinate);
                    return blocker != null
                        ? $"Disharmony: can't land in line with your {blocker.Type}."
                        : "Disharmony: a friendly tile blocks that landing.";
                }

                if (PieceRules.IsWhiteFlower(piece.Type) && BoardUtils.IsRedOnlyGarden(coordinate))
                    return $"{piece.Type} can't end its move in a Red Garden.";

                if (PieceRules.IsRedFlower(piece.Type) && BoardUtils.IsWhiteOnlyGarden(coordinate))
                    return $"{piece.Type} can't end its move in a White Garden.";
            }

            return $"{piece.Type} can't reach that space.";
        }

        private string ExplainSpringPlacementFailure(Player player, PieceType type, int coordinate)
        {
            if (!PieceRules.IsBasicFlower(type))
                return "During spring you can only place basic flowers.";

            if (!IsOnOwnSide(coordinate, player))
                return "During spring, place flowers on your side of the board.";

            if (BoardUtils.IsPort(coordinate))
                return "Spring flowers can't be placed on a port.";

            // Basic Pai Sho: White Gardens = light, Red Gardens = dark; mixed borders allow either.
            if (PieceRules.IsWhiteFlower(type) && BoardUtils.IsRedOnlyGarden(coordinate))
                return "White flowers can't be placed in a Red Garden.";

            if (PieceRules.IsRedFlower(type) && BoardUtils.IsWhiteOnlyGarden(coordinate))
                return "Red flowers can't be placed in a White Garden.";

            return "That flower can't be placed there during spring.";
        }

        private static string ExplainFlowerPlacementFailure(Player player, PieceType type, int coordinate)
        {
            if (!BoardUtils.IsPort(coordinate))
                return $"Basic flowers must enter through a port ({PieceRules.DescribeRequiredPort(type)}).";

            if (!PieceRules.IsValidPortEntry(player, type, coordinate))
                return $"{type} must enter at {PieceRules.DescribeRequiredPort(type)}, not {BoardUtils.GetPortNameForPlayer(coordinate, player)}.";

            return "That port isn't available for this flower.";
        }

        private static string ExplainNonFlowerPlacementFailure(PieceType type, int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return "Tiles can't be placed on a port.";

            return type switch
            {
                PieceType.Knotweed when !PieceRules.IsAwayFromPorts(coordinate) =>
                    "Knotweed must be placed at least one space away from any port.",
                PieceType.Wheel when !PieceRules.IsNeutralGarden(coordinate) =>
                    "Wheels must be placed on a neutral garden tile.",
                PieceType.Boat when !PieceRules.IsColoredGarden(coordinate) =>
                    "Boats must be placed on a light or dark garden tile.",
                _ => "That tile can't be placed here."
            };
        }

        private string ExplainSpecialFlowerPlacementFailure(Player player, PieceType type, int coordinate)
        {
            if (!GameManager.Instance.SpecialTilesUnlocked(player))
                return "Lotus and Dragon Orchid unlock after your 3rd play turn.";

            if (BoardUtils.IsPort(coordinate))
                return "Tiles can't be placed on a port.";

            if (type == PieceType.Lotus && !IsOnOwnSide(coordinate, player))
                return "Lotus must be placed on your side of the board.";

            if (type == PieceType.Orchid && !IsOnOpponentSide(coordinate, player))
                return "Dragon Orchid must be placed on your opponent's side.";

            return "That special flower can't be placed here.";
        }
    }
}
