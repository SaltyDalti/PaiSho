using System.Collections.Generic;
using PaiSho.Pieces;
using PaiSho.Domain;

namespace PaiSho.Game
{
    /// <summary>Builds Domain <see cref="PieceStatus"/> snapshots from live Unity pieces.</summary>
    public static class PieceStatusFactory
    {
        public static PieceStatus FromPiece(Piece piece)
        {
            return new PieceStatus(
                PlacementValidator.ToSeat(piece.Owner),
                piece.Type,
                piece.GetPosition(),
                piece.IsGhost,
                piece.IsBlooming(),
                piece.InHarmony,
                piece.WiltLevel,
                piece.PreviousWiltLevel,
                piece.TurnsSinceMoved,
                piece.FreezeWiltNextTurn,
                piece.PointValue);
        }

        public static List<PieceStatus> FromPieces(IEnumerable<Piece> pieces)
        {
            var list = new List<PieceStatus>();
            if (pieces == null)
                return list;

            foreach (Piece piece in pieces)
            {
                if (piece == null)
                    continue;
                list.Add(FromPiece(piece));
            }

            return list;
        }
    }
}
