using UnityEngine;
using PaiSho.Game;
using PaiSho.Pieces;

namespace PaiSho.Board
{
    /// <summary>
    /// Forgiving board picking — raycast first, then screen-space nearest tile/piece.
    /// </summary>
    public static class BoardPickUtility
    {
        public const float PieceColliderFootprintScale = 1.55f;
        public const float GameplayPointColliderScaleBoost = 1.25f;
        public const float WorldSnapToleranceScale = 1.15f;
        public const float ScreenPickRadiusPixels = 96f;
        public const float ScreenPiecePickRadiusPixels = 120f;
        public const float ScreenHandPickRadiusPixels = 110f;

        public static bool TryResolveCoordinate(
            Camera camera,
            Ray ray,
            Vector2 screenPosition,
            BoardLayout layout,
            BoardManager boardManager,
            out int coordinate)
        {
            coordinate = -1;
            if (camera == null || layout == null || boardManager == null)
                return false;

            if (TryRaycastPick(ray, boardManager, out coordinate))
                return true;

            if (TryPlaneSnapPick(ray, layout, out coordinate))
                return true;

            return TryScreenSpaceNearestPick(camera, screenPosition, layout, boardManager, out coordinate);
        }

        public static bool TryPickBoardPiece(
            Camera camera,
            Vector2 screenPosition,
            BoardManager boardManager,
            out Piece piece)
        {
            piece = null;
            if (camera == null || boardManager == null)
                return false;

            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (TryRaycastPiece(ray, out piece))
                return true;

            return TryScreenSpaceNearestPiece(camera, screenPosition, boardManager, out piece);
        }

        private static bool TryRaycastPick(Ray ray, BoardManager boardManager, out int coordinate)
        {
            coordinate = -1;
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<HandTileHandle>() != null)
                    continue;

                var piece = hit.collider.GetComponentInParent<Piece>();
                if (piece != null && piece.BoardCoordinate >= 0)
                {
                    coordinate = piece.BoardCoordinate;
                    return true;
                }
            }

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<HandTileHandle>() != null)
                    continue;

                var boardPoint = hit.collider.GetComponentInParent<BoardPoint>();
                if (boardPoint != null)
                {
                    coordinate = boardPoint.Coordinate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryRaycastPiece(Ray ray, out Piece piece)
        {
            piece = null;
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<HandTileHandle>() != null)
                    continue;

                var candidate = hit.collider.GetComponentInParent<Piece>();
                if (candidate == null || candidate.BoardCoordinate < 0)
                    continue;

                piece = candidate;
                return true;
            }

            return false;
        }

        private static bool TryPlaneSnapPick(Ray ray, BoardLayout layout, out int coordinate)
        {
            coordinate = -1;
            Vector3 planePoint = layout.GetSurfaceWorldPosition(BoardUtils.MiddleGate);
            var boardPlane = new Plane(Vector3.up, planePoint);
            if (!boardPlane.Raycast(ray, out float planeDistance))
                return false;

            return layout.TryWorldToCoordinate(ray.GetPoint(planeDistance), out coordinate, WorldSnapToleranceScale);
        }

        private static bool TryScreenSpaceNearestPick(
            Camera camera,
            Vector2 screenPosition,
            BoardLayout layout,
            BoardManager boardManager,
            out int coordinate)
        {
            coordinate = -1;
            float bestDistance = ScreenPiecePickRadiusPixels;
            int bestCoordinate = -1;

            foreach (Piece piece in boardManager.GetAllPieces())
            {
                if (piece == null || piece.BoardCoordinate < 0)
                    continue;

                if (!TryGetScreenDistance(camera, screenPosition, boardManager.GetPieceWorldPosition(piece.BoardCoordinate), out float distance))
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCoordinate = piece.BoardCoordinate;
                }
            }

            if (bestCoordinate >= 0)
            {
                coordinate = bestCoordinate;
                return true;
            }

            bestDistance = ScreenPickRadiusPixels;
            for (int i = 0; i < 361; i++)
            {
                if (!BoardUtils.IsValidPointCoordinate(i))
                    continue;

                if (!TryGetScreenDistance(camera, screenPosition, layout.GetSurfaceWorldPosition(i), out float distance))
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCoordinate = i;
                }
            }

            if (bestCoordinate < 0)
                return false;

            coordinate = bestCoordinate;
            return true;
        }

        private static bool TryScreenSpaceNearestPiece(
            Camera camera,
            Vector2 screenPosition,
            BoardManager boardManager,
            out Piece piece)
        {
            piece = null;
            float bestDistance = ScreenPiecePickRadiusPixels;
            Piece bestPiece = null;

            foreach (Piece candidate in boardManager.GetAllPieces())
            {
                if (candidate == null || candidate.BoardCoordinate < 0)
                    continue;

                if (!TryGetScreenDistance(camera, screenPosition, boardManager.GetPieceWorldPosition(candidate.BoardCoordinate), out float distance))
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPiece = candidate;
                }
            }

            if (bestPiece == null)
                return false;

            piece = bestPiece;
            return true;
        }

        private static bool TryGetScreenDistance(Camera camera, Vector2 screenPosition, Vector3 worldPosition, out float distance)
        {
            distance = float.MaxValue;
            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
                return false;

            distance = Vector2.Distance(screenPosition, new Vector2(screen.x, screen.y));
            return true;
        }
    }
}
