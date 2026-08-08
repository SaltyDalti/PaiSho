namespace PaiSho.Game
{
    public readonly struct PushMove
    {
        public readonly Pieces.Piece PushedPiece;
        public readonly int ToCoordinate;

        public PushMove(Pieces.Piece pushedPiece, int toCoordinate)
        {
            PushedPiece = pushedPiece;
            ToCoordinate = toCoordinate;
        }

        public bool IsValid => PushedPiece != null && ToCoordinate >= 0;
    }

    public readonly struct LegalMove
    {
        public readonly int Coordinate;
        public readonly bool IsCapture;
        public readonly Pieces.Piece CaptureTarget;
        public readonly PushMove Push;

        public LegalMove(int coordinate, bool isCapture, Pieces.Piece captureTarget = null, PushMove push = default)
        {
            Coordinate = coordinate;
            IsCapture = isCapture;
            CaptureTarget = captureTarget;
            Push = push;
        }

        public bool HasPush => Push.IsValid;
    }
}
