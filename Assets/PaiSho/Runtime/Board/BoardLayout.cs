using UnityEngine;

namespace PaiSho.Board
{
    public class BoardLayout : MonoBehaviour
    {
        [SerializeField] private float cellSpacing = 0.42f;
        [SerializeField] private float tileHeight;
        [SerializeField] private Transform boardOrigin;
        [Header("Board Visual")]
        [SerializeField] private bool useModelBoard = true;
        [Header("Photo Board Alignment")]
        [SerializeField] private bool usePhotoBoard = true;
        [Tooltip("Fraction of the photo width occupied by the 19×19 point grid span (18 intervals).")]
        [SerializeField] private float photoGridCoverage = 0.84f;
        [Tooltip("Fine-tune UV scale on the board photo.")]
        [SerializeField] private float photoUvScale = 1f;
        [Tooltip("Fine-tune UV offset on the board photo.")]
        [SerializeField] private Vector2 photoUvOffset = Vector2.zero;
        [Tooltip("Texture repeat on neutral garden patches of the 3D board model. Higher = more zoomed in.")]
        [SerializeField] private float neutralGardenTextureTiling = 4f;
        [Tooltip("Brighten neutral garden wood on the 3D board model (0 = unchanged, 1 = white).")]
        [SerializeField] private float neutralGardenLighten = 0.45f;
        [Tooltip("Fine-tune 3D board model position after grid inlay alignment.")]
        [SerializeField] private Vector3 boardModelOffset = Vector3.zero;
        [Tooltip("World Y for the bottom of the 3D board base (tatami floor level).")]
        [SerializeField] private float boardFloorY = -0.12f;
        [Tooltip("Fine-tune intersection spacing after measuring the 3D board (1 = exact).")]
        [SerializeField] private float gridSpacingScale = 0.895176112651825f;
        [SerializeField] private float spacingFineTune = 0.9954929351806641f;
        [SerializeField] private float pointHeightOffset = 0.010704225860536099f;
        [SerializeField] private float boardPointColliderScale = 0.8647887706756592f;
        [SerializeField] private Vector2 gridOffset;
        [SerializeField] private float pieceSurfaceLift;
        [Tooltip("Yaw of the logical grid in degrees. 0 = row axis along local +Z. Set from board gates on calibrate.")]
        [SerializeField] private float gridYawDegrees;

        private float calibratedGridSpan;

        public float CellSpacing => cellSpacing;
        public float TileHeight => tileHeight;
        public float PieceSurfaceLift => pieceSurfaceLift;
        public Transform Origin => boardOrigin != null ? boardOrigin : transform;
        public bool UseModelBoard => useModelBoard;
        public bool UsePhotoBoard => usePhotoBoard;
        public float PhotoGridCoverage => Mathf.Clamp(photoGridCoverage, 0.5f, 1f);
        public float PhotoUvScale => photoUvScale;
        public Vector2 PhotoUvOffset => photoUvOffset;
        public float NeutralGardenTextureTiling => Mathf.Max(1f, neutralGardenTextureTiling);
        public float NeutralGardenLighten => Mathf.Clamp01(neutralGardenLighten);
        public Vector3 BoardModelOffset => boardModelOffset;
        public float BoardFloorY => boardFloorY;
        public float GridSpacingScale => Mathf.Clamp(gridSpacingScale, 0.85f, 1.15f);
        public float SpacingFineTune => Mathf.Clamp(spacingFineTune, 0.85f, 1.15f);
        public float PointHeightOffset => pointHeightOffset;
        public float BoardPointColliderScale => Mathf.Clamp(boardPointColliderScale, 0.5f, 1.1f);
        public Vector2 GridOffset => gridOffset;
        public float GridYawDegrees => gridYawDegrees;

        public void RememberCalibratedGridSpan(float worldGridSpan)
        {
            calibratedGridSpan = Mathf.Max(0f, worldGridSpan);
        }

        public void SetGridSpacingScale(float scale)
        {
            gridSpacingScale = Mathf.Clamp(scale, 0.85f, 1.15f);
            if (calibratedGridSpan > 0.001f)
                CalibrateFromBoardGridSpan(calibratedGridSpan);
        }

        public void SetSpacingFineTune(float multiplier)
        {
            spacingFineTune = Mathf.Clamp(multiplier, 0.85f, 1.15f);
        }

        public void SetPointHeightOffset(float offset)
        {
            pointHeightOffset = offset;
        }

        public void SetBoardPointColliderScale(float scale)
        {
            boardPointColliderScale = Mathf.Clamp(scale, 0.5f, 1.1f);
        }

        public void SetGridOffset(Vector2 offset)
        {
            gridOffset = offset;
        }

        public void SetTunerOverlay(float spacingMultiplier, Vector2 offset, float heightOffset)
        {
            SetSpacingFineTune(spacingMultiplier);
            SetGridOffset(offset);
            SetPointHeightOffset(heightOffset);
        }

        public void SetBoardModelOffset(Vector3 offset)
        {
            boardModelOffset = offset;
        }

        public void SetPieceSurfaceLift(float surfaceLift)
        {
            pieceSurfaceLift = Mathf.Max(0f, surfaceLift);
        }

        public void SetPieceMetrics(float spacing, float surfaceHeight, float surfaceLift)
        {
            cellSpacing = Mathf.Max(0.05f, spacing);
            tileHeight = surfaceHeight;
            pieceSurfaceLift = Mathf.Max(0f, surfaceLift);
        }

        public float GridSpan => BoardUtils.GridIntervals * cellSpacing;

        public float GetPhotoBoardWorldSize()
        {
            return GridSpan / PhotoGridCoverage;
        }

        public void CalibrateFromBoardGridSpan(float worldGridSpan)
        {
            if (worldGridSpan <= 0.001f)
                return;

            cellSpacing = worldGridSpan / BoardUtils.GridIntervals * GridSpacingScale;
        }

        public void SetBoardSurfaceHeight(float surfaceHeight)
        {
            tileHeight = surfaceHeight;
        }

        public void SetGridYawDegrees(float yawDegrees)
        {
            gridYawDegrees = yawDegrees;
        }

        public Vector3 CoordinateToWorld(int coordinate)
        {
            int row = BoardUtils.GetRow(coordinate);
            int col = BoardUtils.GetColumn(coordinate);
            Transform origin = Origin;

            float spacing = cellSpacing * spacingFineTune;
            float localX = (col - 9f) * spacing + gridOffset.x;
            float localZ = (row - 9f) * spacing + gridOffset.y;
            RotateGrid(localX, localZ, out float x, out float z);
            return origin.TransformPoint(new Vector3(x, tileHeight + pointHeightOffset, z));
        }

        public Vector3 GetSurfaceWorldPosition(int coordinate, float extraLift = 0f)
        {
            Vector3 point = CoordinateToWorld(coordinate);
            point.y += pieceSurfaceLift + extraLift;
            return point;
        }

        public bool TryWorldToCoordinate(Vector3 worldPosition, out int coordinate) =>
            TryWorldToCoordinate(worldPosition, out coordinate, 0.65f);

        public bool TryWorldToCoordinate(Vector3 worldPosition, out int coordinate, float toleranceScale)
        {
            Transform origin = Origin;
            Vector3 local = origin.InverseTransformPoint(worldPosition);
            InverseRotateGrid(local.x - gridOffset.x, local.z - gridOffset.y, out float gridX, out float gridZ);

            float spacing = cellSpacing * spacingFineTune;
            int col = Mathf.RoundToInt(gridX / spacing + 9f);
            int row = Mathf.RoundToInt(gridZ / spacing + 9f);
            coordinate = row * BoardUtils.GridSize + col;

            if (!BoardUtils.IsValidPointCoordinate(coordinate))
                return false;

            Vector3 snapped = GetSurfaceWorldPosition(coordinate);
            float maxDistance = cellSpacing * Mathf.Clamp(toleranceScale, 0.5f, 1.1f);
            return Vector3.Distance(new Vector3(worldPosition.x, snapped.y, worldPosition.z), snapped) <= maxDistance;
        }

        private void RotateGrid(float localX, float localZ, out float x, out float z)
        {
            if (Mathf.Abs(gridYawDegrees) < 0.001f)
            {
                x = localX;
                z = localZ;
                return;
            }

            float rad = gridYawDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            x = localX * cos - localZ * sin;
            z = localX * sin + localZ * cos;
        }

        private void InverseRotateGrid(float x, float z, out float localX, out float localZ)
        {
            if (Mathf.Abs(gridYawDegrees) < 0.001f)
            {
                localX = x;
                localZ = z;
                return;
            }

            float rad = -gridYawDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            localX = x * cos - z * sin;
            localZ = x * sin + z * cos;
        }

        /// <summary>Axis-aligned world bounds covering the playable intersection diamond.</summary>
        public Bounds GetWorldBounds()
        {
            int[] rim = { 46, 314, 48, 312, 28, 332, 172, 188, 180 };
            bool first = true;
            Bounds bounds = default;

            foreach (int coordinate in rim)
            {
                if (!BoardUtils.IsValidPointCoordinate(coordinate))
                    continue;

                Vector3 point = GetSurfaceWorldPosition(coordinate);
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }

            if (first)
            {
                Transform origin = Origin;
                float half = GridSpan * 0.5f;
                bounds = new Bounds(
                    origin.position + Vector3.up * (tileHeight + pieceSurfaceLift),
                    new Vector3(GridSpan, 0.5f, GridSpan));
            }

            bounds.Expand(new Vector3(CellSpacing * 0.35f, CellSpacing * 0.25f, CellSpacing * 0.35f));
            return bounds;
        }
    }
}
