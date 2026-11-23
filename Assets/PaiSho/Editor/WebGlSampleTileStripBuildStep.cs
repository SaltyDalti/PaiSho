#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using PaiSho.Game;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// WebGL player bake: replace hand-tray SampleTile piece meshes with empty pose anchors
    /// so Safari never deserializes fourteen tile prefabs during callMain (HJ-37).
    /// Local TRS is preserved for runtime <c>ApplySampleTilePose</c>.
    /// </summary>
    public sealed class WebGlSampleTileStripBuildStep : IProcessSceneWithReport
    {
        public int callbackOrder => 100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL)
                return;

            int stripped = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                stripped += StripUnder(roots[i].transform);

            if (stripped > 0)
            {
                Debug.Log(
                    $"[WebGL] Stripped {stripped} hand-tray SampleTile mesh hierarchies in " +
                    $"'{scene.path}' (pose anchors kept).");
            }
        }

        private static int StripUnder(Transform root)
        {
            if (root == null)
                return 0;

            int count = 0;
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                if (current == null)
                    continue;

                if (current.name == HandTraySlotMarker.SampleTileName &&
                    GameBoardSetup.IsHandTrayAuthoringHierarchy(current))
                {
                    count += ReplaceWithPoseAnchor(current);
                    continue;
                }

                for (int i = 0; i < current.childCount; i++)
                    queue.Enqueue(current.GetChild(i));
            }

            return count;
        }

        private static int ReplaceWithPoseAnchor(Transform sample)
        {
            if (sample == null || sample.parent == null)
                return 0;

            Transform parent = sample.parent;
            Vector3 localPosition = sample.localPosition;
            Quaternion localRotation = sample.localRotation;
            Vector3 localScale = sample.localScale;
            int sibling = sample.GetSiblingIndex();

            // Prefer in-place hollow when already a plain GO; PrefabInstances need replace.
            if (PrefabUtility.IsPartOfPrefabInstance(sample.gameObject) ||
                HandTrayLayoutUtility.HasRenderableMesh(sample))
            {
                Object.DestroyImmediate(sample.gameObject);

                var anchor = new GameObject(HandTraySlotMarker.SampleTileName);
                Transform t = anchor.transform;
                t.SetParent(parent, false);
                t.SetSiblingIndex(Mathf.Clamp(sibling, 0, parent.childCount));
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = localScale;
                return 1;
            }

            HandTrayLayoutUtility.HollowSampleTile(sample);
            return 1;
        }
    }
}
#endif
