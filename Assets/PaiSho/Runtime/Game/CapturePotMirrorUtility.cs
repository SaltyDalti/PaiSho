using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Copies capture-pot layout from host to opponent by negating local X (Y and Z unchanged).</summary>
    public static class CapturePotMirrorUtility
    {
        public static int MirrorHostToOpponent(Transform hostRoot, Transform opponentRoot)
        {
            if (hostRoot == null || opponentRoot == null)
                return 0;

            int count = 0;
            ApplyMirroredLocalTransform(hostRoot, opponentRoot);
            count++;

            foreach (Transform hostTransform in hostRoot.GetComponentsInChildren<Transform>(true))
            {
                if (hostTransform == hostRoot || IsSampleTileTransform(hostTransform))
                    continue;

                string relativePath = GetRelativePath(hostTransform, hostRoot);
                if (string.IsNullOrEmpty(relativePath))
                    continue;

                Transform opponentTransform = opponentRoot.Find(relativePath);
                if (opponentTransform == null)
                    continue;

                ApplyMirroredLocalTransform(hostTransform, opponentTransform);
                count++;
            }

            return count;
        }

        public static void ApplyMirroredLocalTransform(Transform source, Transform destination)
        {
            Vector3 local = source.localPosition;
            destination.localPosition = new Vector3(-local.x, local.y, local.z);
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        public static bool IsSampleTileTransform(Transform transform)
        {
            return transform.name == CapturePotStackCatalog.SampleTileName ||
                   transform.GetComponent<CapturePotSampleTile>() != null;
        }

        public static string GetRelativePath(Transform child, Transform root)
        {
            if (child == null || root == null || child == root)
                return string.Empty;

            var segments = new System.Collections.Generic.List<string>(4);
            Transform current = child;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            if (current != root)
                return string.Empty;

            segments.Reverse();
            return string.Join("/", segments);
        }
    }
}
