#if UNITY_EDITOR
using UnityEditor;
using PaiSho.Game;

namespace PaiSho.EditorTools
{
    /// <summary>Always draw slot tile gizmos in the Scene view (no mesh materials).</summary>
    public static class HandTraySlotMarkerDrawer
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
        private static void DrawSlotGizmo(HandTraySlotMarker marker, GizmoType type)
        {
            if (marker == null)
                return;

            marker.DrawEditorGizmo(type == GizmoType.Selected || type == GizmoType.Active);
        }
    }
}
#endif
