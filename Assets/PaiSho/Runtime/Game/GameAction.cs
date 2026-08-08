using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum GameActionKind
    {
        Move,
        Place,
        Revive,
        Freeze,
        BoatLoad,
        BoatUnload,
        WheelRotate
    }

    public readonly struct GameAction
    {
        public readonly GameActionKind Kind;
        public readonly Player Player;
        public readonly Piece Piece;
        public readonly PieceType PlaceType;
        public readonly int Coordinate;

        private GameAction(
            GameActionKind kind,
            Player player,
            Piece piece,
            PieceType placeType,
            int coordinate)
        {
            Kind = kind;
            Player = player;
            Piece = piece;
            PlaceType = placeType;
            Coordinate = coordinate;
        }

        public static GameAction Move(Piece piece, int coordinate)
        {
            return new GameAction(GameActionKind.Move, piece.Owner, piece, default, coordinate);
        }

        public static GameAction Place(Player player, PieceType type, int coordinate)
        {
            return new GameAction(GameActionKind.Place, player, null, type, coordinate);
        }

        public static GameAction Revive(Player player, Piece piece)
        {
            return new GameAction(GameActionKind.Revive, player, piece, default, piece?.BoardCoordinate ?? -1);
        }

        public static GameAction Freeze(Player player, Piece piece)
        {
            return new GameAction(GameActionKind.Freeze, player, piece, default, piece?.BoardCoordinate ?? -1);
        }

        /// <summary>Boat load: <paramref name="boat"/> loads the flower currently at <paramref name="passengerCoordinate"/>.</summary>
        public static GameAction BoatLoad(Piece boat, int passengerCoordinate)
        {
            return new GameAction(GameActionKind.BoatLoad, boat.Owner, boat, default, passengerCoordinate);
        }

        /// <summary>Boat unload: <paramref name="boat"/> unloads cargo onto <paramref name="coordinate"/>.</summary>
        public static GameAction BoatUnload(Piece boat, int coordinate)
        {
            return new GameAction(GameActionKind.BoatUnload, boat.Owner, boat, default, coordinate);
        }

        public static GameAction WheelRotate(Piece wheel)
        {
            return new GameAction(
                GameActionKind.WheelRotate,
                wheel.Owner,
                wheel,
                default,
                wheel?.BoardCoordinate ?? -1);
        }

        public override string ToString()
        {
            return Kind switch
            {
                GameActionKind.Move => $"Move {Piece?.Type} to {Coordinate}",
                GameActionKind.Place => $"Place {PlaceType} at {Coordinate}",
                GameActionKind.Revive => $"Revive {Piece?.Type} at {Coordinate}",
                GameActionKind.Freeze => $"Freeze {Piece?.Type} at {Coordinate}",
                GameActionKind.BoatLoad => $"Boat load passenger at {Coordinate}",
                GameActionKind.BoatUnload => $"Boat unload at {Coordinate}",
                GameActionKind.WheelRotate => $"Wheel rotate {Piece?.Type}",
                _ => "Unknown action"
            };
        }
    }
}
