using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Single apply-action path for human-driven helpers, AI, and headless self-play.
    /// Presentation still lives in <see cref="TileSelector"/>; this is the only call gate.
    /// </summary>
    public static class ActionApplicator
    {
        public static bool TryApply(GameAction action)
        {
            if (TileSelector.Instance == null)
                return false;

            switch (action.Kind)
            {
                case GameActionKind.Move:
                    return TileSelector.Instance.TryMoveTile(action.Piece, action.Coordinate);
                case GameActionKind.Place:
                    return TileSelector.Instance.TryPlaceTile(action.Player, action.PlaceType, action.Coordinate);
                case GameActionKind.Revive:
                    return TileSelector.Instance.TryMomentumRevive(action.Player, action.Piece);
                case GameActionKind.Freeze:
                    return TileSelector.Instance.TryMomentumFreeze(action.Player, action.Piece);
                case GameActionKind.BoatLoad:
                    return TileSelector.Instance.TryBoatLoad(action.Player, action.Piece, action.Coordinate);
                case GameActionKind.BoatUnload:
                    return TileSelector.Instance.TryBoatUnload(action.Player, action.Piece, action.Coordinate);
                case GameActionKind.WheelRotate:
                    return TileSelector.Instance.TryRotateWheel(action.Player, action.Piece);
                default:
                    return false;
            }
        }
    }
}
