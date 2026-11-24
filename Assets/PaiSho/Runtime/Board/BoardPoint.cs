using UnityEngine;
using PaiSho;
using PaiSho.Game;
using PaiSho.Presentation;

namespace PaiSho.Board
{
    [RequireComponent(typeof(Collider))]
    public class BoardPoint : MonoBehaviour
    {
        private const string VisualName = "TunerMarker";
        private const string LabelName = "CoordLabel";

        public int Coordinate { get; private set; }

        private GameObject visual;
        private Renderer visualRenderer;
        private TextMesh label;
        private static Material markerMaterial;
        private static Material portMarkerMaterial;
        private static Material lightGardenMaterial;
        private static Material darkGardenMaterial;
        private static Material mixedGardenMaterial;
        private static Material neutralGardenMaterial;

        public void Initialize(int coordinate, BoardLayout layout)
        {
            Coordinate = coordinate;
            gameObject.name = $"Point_{coordinate}";
            Refresh(layout, null);
        }

        public void Refresh(BoardLayout layout, BoardPointTunerSettings settings)
        {
            if (layout == null)
                return;

            transform.position = layout.GetSurfaceWorldPosition(Coordinate, 0.003f);
            ApplyCollider(layout, settings);
            ApplyVisual(layout, settings);
        }

        private void LateUpdate()
        {
            if (label == null || !label.gameObject.activeInHierarchy)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            label.transform.rotation = Quaternion.LookRotation(
                label.transform.position - camera.transform.position,
                Vector3.up);
        }

        private void ApplyCollider(BoardLayout layout, BoardPointTunerSettings settings)
        {
            float scale = settings != null ? settings.colliderScale : layout.BoardPointColliderScale;
            if (settings == null)
                scale *= BoardPickUtility.GameplayPointColliderScaleBoost;
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider>();

            float size = layout.CellSpacing * scale;
            float height = Mathf.Max(0.08f, size * 0.18f);
            collider.size = new Vector3(size, height, size);
            collider.center = new Vector3(0f, height * 0.35f, 0f);
            collider.isTrigger = false;
            // Occupied intersections are not placement targets; hide the pick volume with the marker.
            collider.enabled = !IsOccupied();
        }

        private bool IsOccupied()
        {
            if (BoardManager.Instance == null)
                return false;
            return BoardManager.Instance.GetPieceAt(Coordinate) != null;
        }

        private void ApplyVisual(BoardLayout layout, BoardPointTunerSettings settings)
        {
            BoardPointTunerSettings active = settings ?? BoardPointRuntimeStyle.Gameplay;
            // Occupied points always hide markers (F8/dev included) so the tile covers the disc.
            bool show = active.showMarkers && !IsOccupied();
            if (!show)
            {
                if (visual != null)
                    visual.SetActive(false);
                if (label != null)
                    label.gameObject.SetActive(false);
                return;
            }

            EnsureVisual(settings == null);
            visual.SetActive(true);
            visual.layer = 0;

            float diameter = Mathf.Max(0.08f, layout.CellSpacing * active.markerDiameterScale);
            bool isPort = BoardUtils.IsPort(Coordinate);
            if (isPort)
                diameter *= 1.35f;

            bool gameplay = settings == null;
            // Flat 0.008 discs z-fought / vanished into wood on WebGL; keep a readable puck.
            float discHeight = gameplay
                ? Mathf.Max(0.022f, diameter * 0.18f)
                : diameter * 0.35f;
            visual.transform.localScale = new Vector3(diameter, discHeight, diameter);
            visual.transform.localPosition = Vector3.up * Mathf.Max(0.04f, active.markerHeightOffset);

            if (visualRenderer != null)
            {
                visualRenderer.enabled = true;
                visualRenderer.sharedMaterial = ResolveMarkerMaterial(active, gameplay, isPort);
                if (gameplay)
                    WoodTheme.EnableTransparentSurface(visualRenderer);
            }

            bool showLabel = active.showLabels;
            if (!showLabel)
            {
                if (label != null)
                    label.gameObject.SetActive(false);
                return;
            }

            EnsureLabel();
            label.gameObject.SetActive(true);
            label.text = BuildLabelText();
            label.color = isPort ? Color.yellow : Color.white;
            float labelSize = Mathf.Clamp(layout.CellSpacing * 0.085f, 0.035f, 0.08f);
            label.characterSize = isPort ? labelSize * 1.25f : labelSize;
            label.transform.localPosition = Vector3.up * (active.markerHeightOffset + diameter * 0.9f);
        }

        private Material ResolveMarkerMaterial(BoardPointTunerSettings settings, bool gameplay, bool isPort)
        {
            settings.SyncMarkerColorAlpha();

            if (isPort)
            {
                EnsurePortMaterial(gameplay);
                return portMarkerMaterial;
            }

            if (settings.colorByGarden)
            {
                GardenType garden = BoardUtils.GetGardenType(Coordinate);
                EnsureGardenMaterials(gameplay);
                if (garden == GardenType.LightGarden)
                    return lightGardenMaterial;
                if (garden == GardenType.DarkGarden)
                    return darkGardenMaterial;
                if (garden == GardenType.MixedGarden)
                    return mixedGardenMaterial;
                if (garden == GardenType.NeutralGarden)
                    return neutralGardenMaterial;
            }

            EnsureMarkerMaterial(settings.markerColor, gameplay);
            return markerMaterial;
        }

        private void EnsureVisual(bool gameplayDisc = false)
        {
            if (visual != null)
            {
                bool wantDisc = gameplayDisc && visual.name != "IntersectionDisc";
                bool wantSphere = !gameplayDisc && visual.name != VisualName;
                if (wantDisc || wantSphere)
                {
                    Destroy(visual);
                    visual = null;
                    visualRenderer = null;
                }
                else
                {
                    return;
                }
            }

            // Cylinder → CapsuleCollider; keep Physics types alive under WebGL stripEngineCode.
            WebGlPhysicsPreserve.EnsureCollidersPreserved();

            PrimitiveType primitive = gameplayDisc ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
            try
            {
                visual = GameObject.CreatePrimitive(primitive);
            }
            catch (System.Exception)
            {
                visual = null;
            }

            // Fallback if CreatePrimitive failed (stripped CapsuleCollider) — Cube only needs BoxCollider.
            if (visual == null || visual.GetComponent<Renderer>() == null)
            {
                if (visual != null)
                    Destroy(visual);
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            visual.name = gameplayDisc ? "IntersectionDisc" : VisualName;
            visual.layer = 0;
            visual.transform.SetParent(transform, false);
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                visualRenderer.enabled = true;
                visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                visualRenderer.receiveShadows = false;
                visualRenderer.material.renderQueue = 3100;
            }
        }

        private string BuildLabelText()
        {
            if (BoardUtils.IsPort(Coordinate))
            {
                string port = Coordinate switch
                {
                    BoardUtils.SouthGate => "S/Home",
                    BoardUtils.NorthGate => "N/Foreign",
                    BoardUtils.EastGate => "E",
                    BoardUtils.WestGate => "W",
                    BoardUtils.MiddleGate => "Mid",
                    _ => "Port"
                };
                return $"{port}\n{Coordinate}";
            }

            GardenType garden = BoardUtils.GetGardenType(Coordinate);
            string gardenTag = garden switch
            {
                GardenType.LightGarden => "L",
                GardenType.DarkGarden => "D",
                GardenType.MixedGarden => "M",
                _ => "N"
            };
            return $"{Coordinate}\n{gardenTag}";
        }

        private void EnsureLabel()
        {
            if (label != null)
                return;

            var labelObject = new GameObject(LabelName);
            labelObject.transform.SetParent(transform, false);
            label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 0.04f;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
        }

        private static void EnsureMarkerMaterial(Color color, bool gameplay)
        {
            if (markerMaterial == null)
                markerMaterial = WoodTheme.CreateMarkerMaterial(color, gameplay);

            Color c = color;
            if (gameplay)
                c.a = Mathf.Max(c.a, 0.85f);

            // Brighter emission so cyan discs read on WebGL wood (visibility pass 0.1.25).
            WoodTheme.UpdateMarkerMaterial(markerMaterial, c, gameplay ? 1.35f : 1.2f);
        }

        private static void EnsurePortMaterial(bool gameplay)
        {
            var color = gameplay
                ? new Color(0.92f, 0.76f, 0.32f, 0.75f)
                : new Color(1f, 0.85f, 0.15f, 0.95f);

            if (portMarkerMaterial == null)
                portMarkerMaterial = WoodTheme.CreateMarkerMaterial(color, gameplay);

            WoodTheme.UpdateMarkerMaterial(portMarkerMaterial, color, gameplay ? 1f : 1.4f);
        }

        private static void EnsureGardenMaterials(bool gameplay)
        {
            if (lightGardenMaterial == null)
            {
                var light = gameplay
                    ? new Color(0.95f, 0.88f, 0.62f, 0.62f)
                    : new Color(0.95f, 0.9f, 0.55f, 0.9f);
                lightGardenMaterial = WoodTheme.CreateMarkerMaterial(light, gameplay);
            }

            if (darkGardenMaterial == null)
            {
                var dark = gameplay
                    ? new Color(0.55f, 0.38f, 0.3f, 0.62f)
                    : new Color(0.35f, 0.55f, 0.95f, 0.9f);
                darkGardenMaterial = WoodTheme.CreateMarkerMaterial(dark, gameplay);
            }

            if (mixedGardenMaterial == null)
            {
                var mixed = gameplay
                    ? new Color(0.72f, 0.58f, 0.42f, 0.58f)
                    : new Color(0.75f, 0.4f, 0.95f, 0.9f);
                mixedGardenMaterial = WoodTheme.CreateMarkerMaterial(mixed, gameplay);
            }

            if (neutralGardenMaterial == null)
            {
                var neutral = gameplay
                    ? new Color(0.38f, 0.34f, 0.3f, 0.5f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.9f);
                neutralGardenMaterial = WoodTheme.CreateMarkerMaterial(neutral, gameplay);
            }
        }
    }
}
