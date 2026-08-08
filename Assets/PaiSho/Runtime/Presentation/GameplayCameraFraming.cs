using UnityEngine;
using PaiSho.Board;
using PaiSho.Game;

namespace PaiSho
{
    /// <summary>Frames the 3D board plus the host hand rack in one stable gameplay shot.</summary>
    public static class GameplayCameraFraming
    {
        private const float HudBottomReserve = 0.16f;
        private const float BoundsPadding = 1.22f;

        public static bool TryBuildGameplayBounds(BoardLayout layout, out Bounds bounds)
        {
            bounds = default;
            if (layout == null)
                return false;

            bounds = layout.GetWorldBounds();

            if (HandTrayController.Instance != null &&
                HandTrayController.Instance.TryGetPlayerTrayWorldBounds(Player.Host, out Bounds trayBounds))
            {
                bounds.Encapsulate(trayBounds);
            }

            return true;
        }

        public static void ApplyTo(BoardCameraController controller, Camera camera, BoardLayout layout)
        {
            if (controller == null || camera == null || layout == null)
                return;

            if (!TryBuildGameplayBounds(layout, out Bounds bounds))
                return;

            controller.ConfigureGameplayView(camera, layout, bounds, HudBottomReserve, BoundsPadding);
        }

        public static float ComputeFitDistance(Camera camera, Bounds bounds, float pitchDegrees, float padding)
        {
            float verticalFovRad = camera.fieldOfView * Mathf.Deg2Rad;
            float halfVert = verticalFovRad * 0.5f;
            float halfHoriz = Mathf.Atan(Mathf.Tan(halfVert) * Mathf.Max(0.5f, camera.aspect));

            Vector3 size = bounds.size * padding;
            float pitchRad = pitchDegrees * Mathf.Deg2Rad;
            float depthOnView = size.z * Mathf.Cos(pitchRad) + size.y * Mathf.Sin(pitchRad);
            float verticalNeed = Mathf.Max(size.y, depthOnView);
            float horizontalNeed = size.x;

            float distanceForHeight = verticalNeed * 0.5f / Mathf.Tan(halfVert);
            float distanceForWidth = horizontalNeed * 0.5f / Mathf.Tan(halfHoriz);
            return Mathf.Max(distanceForHeight, distanceForWidth);
        }
    }
}
