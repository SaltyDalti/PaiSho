#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using PaiSho.Board;

namespace PaiSho.EditorTools
{
    /// <summary>Play-mode probe: logs every renderer/collider near the middle gate (coord 180).</summary>
    public static class MiddleGateProbe
    {
        [MenuItem("Pai Sho/Debug/Log Middle Gate Objects (Play Mode)")]
        private static void LogMiddleGateObjects()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MiddleGateProbe] Enter Play mode first, then run this menu item.");
                return;
            }

            var layout = Object.FindAnyObjectByType<BoardLayout>();
            if (layout == null)
            {
                Debug.LogWarning("[MiddleGateProbe] No BoardLayout found.");
                return;
            }

            Vector3 gate = layout.GetSurfaceWorldPosition(BoardUtils.MiddleGate);
            float cell = layout.CellSpacing;
            float searchRadius = cell * 0.75f;

            var report = new StringBuilder();
            report.AppendLine($"[MiddleGateProbe] Middle gate coord={BoardUtils.MiddleGate} world={gate}");
            report.AppendLine($"  cellSpacing={cell:F4}  searchRadius={searchRadius:F4}");
            report.AppendLine();

            Transform point180 = FindDeep("Point_180");
            if (point180 != null)
            {
                report.AppendLine("=== Point_180 (board click collider) ===");
                AppendTransformReport(report, point180, gate);
                var box = point180.GetComponent<BoxCollider>();
                if (box != null)
                {
                    report.AppendLine($"  BoxCollider size={box.size} center={box.center} enabled={box.enabled}");
                    report.AppendLine("  NOTE: BoxCollider has no mesh — it should be invisible unless Physics debug is on.");
                }

                Transform marker = point180.Find("TunerMarker");
                if (marker != null)
                    AppendTransformReport(report, marker, gate);
            }
            else
            {
                report.AppendLine("Point_180 not found under BoardPoints.");
            }

            report.AppendLine();
            report.AppendLine("=== Renderers near middle gate ===");
            int rendererCount = 0;
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                float dist = HorizontalDistance(renderer.bounds.center, gate);
                if (dist > searchRadius && !renderer.bounds.Contains(gate))
                    continue;

                rendererCount++;
                Material mat = renderer.sharedMaterial;
                string matName = mat != null ? mat.name : "NULL";
                string shader = mat != null && mat.shader != null ? mat.shader.name : "NULL";
                report.AppendLine(
                    $"  [{renderer.gameObject.name}] path={GetPath(renderer.transform)}");
                report.AppendLine(
                    $"    bounds={renderer.bounds.size} center={renderer.bounds.center} distXZ={dist:F4}");
                report.AppendLine($"    material={matName} shader={shader}");
            }

            if (rendererCount == 0)
                report.AppendLine("  (none)");

            report.AppendLine();
            report.AppendLine("=== Known runtime roots ===");
            LogRoot(report, "BoardPoints");
            LogRoot(report, "LegalMoveMarkers");
            LogRoot(report, "BoardModelSurface");
            LogRoot(report, "TableWood");
            LogRoot(report, "TablePedestal");
            LogRoot(report, "GardenPatches");
            LogRoot(report, "BoardPhotoSurface");

            Debug.Log(report.ToString());
        }

        private static void LogRoot(StringBuilder report, string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                report.AppendLine($"  {name}: (missing)");
                return;
            }

            int renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            report.AppendLine($"  {name}: active={root.activeInHierarchy} renderers={renderers}");
        }

        private static Transform FindDeep(string objectName)
        {
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (obj.name == objectName)
                    return obj.transform;
            }

            return null;
        }

        private static void AppendTransformReport(StringBuilder report, Transform transform, Vector3 gate)
        {
            report.AppendLine($"  {transform.name} active={transform.gameObject.activeInHierarchy}");
            report.AppendLine($"    path={GetPath(transform)}");
            report.AppendLine($"    pos={transform.position} distXZ={HorizontalDistance(transform.position, gate):F4}");
            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
                report.AppendLine($"    Renderer enabled={renderer.enabled} material={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "NULL")}");
        }

        private static string GetPath(Transform transform)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
#endif
