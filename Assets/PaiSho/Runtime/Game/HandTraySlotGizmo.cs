using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho.DevTools;

namespace PaiSho.Game
{
    /// <summary>Runtime translate/rotate gizmo for F9 hand-tray slot editing.</summary>
    public class HandTraySlotGizmo : MonoBehaviour
    {
        public enum GizmoMode
        {
            Translate,
            Rotate
        }

        private const float PickThreshold = 0.14f;
        private const float RotateSensitivity = 0.45f;
        private const float GizmoScale = 0.22f;

        private static readonly Color[] AxisColors =
        {
            new Color(0.95f, 0.25f, 0.2f),
            new Color(0.3f, 0.9f, 0.35f),
            new Color(0.3f, 0.55f, 1f)
        };

        private Transform gizmoRoot;
        private readonly Transform[] translateHandles = new Transform[3];
        private readonly Transform[] rotateHandles = new Transform[3];
        private GizmoMode mode = GizmoMode.Translate;
        private int selectedSlot;
        private int activeAxis = -1;
        private bool dragging;
        private Vector3 dragAxisWorld;
        private Vector3 dragStartWorldOnAxis;
        private Vector3 dragStartLocalPosition;
        private Quaternion dragStartLocalRotation;
        private Vector2 dragStartPointer;
        private float dragStartAngle;
        private HandTrayTuner tuner;

        public GizmoMode Mode => mode;
        public int SelectedSlot => selectedSlot;
        public bool IsDragging => dragging;

        public void Bind(HandTrayTuner owner)
        {
            tuner = owner;
        }

        public void SetMode(GizmoMode value)
        {
            mode = value;
            UpdateHandleVisibility();
        }

        public void SetSelectedSlot(int slot)
        {
            selectedSlot = Mathf.Clamp(slot, 0, HandTrayTunerSettings.MaxSlots - 1);
        }

        public void Tick(bool active, HandTrayTunerSettings settings)
        {
            if (!active || settings == null || HandTrayController.Instance == null)
            {
                if (gizmoRoot != null)
                    gizmoRoot.gameObject.SetActive(false);
                dragging = false;
                activeAxis = -1;
                return;
            }

            EnsureGizmoObjects();
            settings.useManualSlotPositions = true;
            settings.previewAllSlots = true;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    SetMode(GizmoMode.Translate);
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    SetMode(GizmoMode.Rotate);
            }

            if (!HandTrayController.Instance.TryGetSlotHandle(settings.editingPlayer, selectedSlot, out HandTileHandle handle) ||
                handle == null)
            {
                gizmoRoot.gameObject.SetActive(false);
                return;
            }

            gizmoRoot.gameObject.SetActive(true);
            PositionGizmo(handle.transform);

            if (Mouse.current == null || Camera.main == null)
                return;

            Vector2 pointer = Mouse.current.position.ReadValue();

            if (!dragging)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && !DevTunerGui.IsPointerOverPanel(pointer))
                {
                    if (TryPickAxis(pointer, out int axis))
                    {
                        BeginDrag(handle, axis, pointer);
                    }
                    else if (TryPickSlotTile(pointer, settings.editingPlayer, out int slot))
                    {
                        selectedSlot = slot;
                        tuner?.NotifySlotSelected(slot);
                    }
                }
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                ContinueDrag(handle, pointer);
            }
            else
            {
                EndDrag(handle, settings);
            }
        }

        private void EnsureGizmoObjects()
        {
            if (gizmoRoot != null)
                return;

            gizmoRoot = new GameObject("HandTraySlotGizmo").transform;
            gizmoRoot.SetParent(transform, false);
            gizmoRoot.gameObject.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                translateHandles[i] = CreateAxisHandle($"Move_{AxisName(i)}", AxisColors[i], isRing: false, i);
                rotateHandles[i] = CreateAxisHandle($"Rotate_{AxisName(i)}", AxisColors[i], isRing: true, i);
            }

            UpdateHandleVisibility();
        }

        private Transform CreateAxisHandle(string name, Color color, bool isRing, int axisIndex)
        {
            var go = GameObject.CreatePrimitive(isRing ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(gizmoRoot, false);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("Sprites/Default");
                renderer.material = new Material(shader);
                renderer.material.color = color;
            }

            var marker = go.AddComponent<HandTrayGizmoHandle>();
            marker.AxisIndex = axisIndex;
            marker.IsRotate = isRing;
            return go.transform;
        }

        private static string AxisName(int axis) => axis switch { 0 => "X", 1 => "Y", _ => "Z" };

        private void UpdateHandleVisibility()
        {
            if (gizmoRoot == null)
                return;

            for (int i = 0; i < 3; i++)
            {
                if (translateHandles[i] != null)
                    translateHandles[i].gameObject.SetActive(mode == GizmoMode.Translate);
                if (rotateHandles[i] != null)
                    rotateHandles[i].gameObject.SetActive(mode == GizmoMode.Rotate);
            }
        }

        private void PositionGizmo(Transform target)
        {
            float scale = GizmoScale;
            if (Camera.main != null)
            {
                float distance = Vector3.Distance(Camera.main.transform.position, target.position);
                scale = Mathf.Clamp(distance * 0.055f, 0.12f, 0.42f);
            }

            gizmoRoot.position = target.position;
            gizmoRoot.rotation = target.rotation;
            gizmoRoot.localScale = Vector3.one * scale;

            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = AxisDirection(i);
                if (translateHandles[i] != null)
                {
                    translateHandles[i].localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                    translateHandles[i].localPosition = axis * 0.55f;
                    translateHandles[i].localScale = new Vector3(0.12f, 0.55f, 0.12f);
                }

                if (rotateHandles[i] != null)
                {
                    rotateHandles[i].localRotation = Quaternion.FromToRotation(Vector3.up, axis);
                    rotateHandles[i].localPosition = Vector3.zero;
                    rotateHandles[i].localScale = new Vector3(1.15f, 0.02f, 1.15f);
                }
            }
        }

        private static Vector3 AxisDirection(int axis)
        {
            return axis switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                _ => Vector3.forward
            };
        }

        private bool TryPickAxis(Vector2 pointer, out int axis)
        {
            axis = -1;
            if (Camera.main == null)
                return false;

            Ray ray = Camera.main.ScreenPointToRay(pointer);
            float best = PickThreshold;
            Transform[] handles = mode == GizmoMode.Translate ? translateHandles : rotateHandles;

            for (int i = 0; i < 3; i++)
            {
                Transform handle = handles[i];
                if (handle == null || !handle.gameObject.activeInHierarchy)
                    continue;

                Vector3 axisOrigin = gizmoRoot.position;
                Vector3 axisDir = gizmoRoot.TransformDirection(AxisDirection(i)).normalized;
                float dist = DistanceRayToLine(ray, axisOrigin, axisDir, out _, out float alongAxis);
                float pickRadius = mode == GizmoMode.Translate
                    ? Mathf.Clamp(gizmoRoot.lossyScale.x * 0.18f, 0.04f, 0.2f)
                    : Mathf.Clamp(gizmoRoot.lossyScale.x * 0.55f, 0.08f, 0.35f);

                if (mode == GizmoMode.Rotate && (alongAxis < -pickRadius || alongAxis > pickRadius))
                    continue;

                if (dist < pickRadius && dist < best)
                {
                    best = dist;
                    axis = i;
                }
            }

            return axis >= 0;
        }

        private static float DistanceRayToLine(
            Ray ray,
            Vector3 lineOrigin,
            Vector3 lineDir,
            out float rayT,
            out float lineS)
        {
            Vector3 w0 = ray.origin - lineOrigin;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = Vector3.Dot(ray.direction, lineDir);
            float c = Vector3.Dot(lineDir, lineDir);
            float d = Vector3.Dot(ray.direction, w0);
            float e = Vector3.Dot(lineDir, w0);
            float denom = a * c - b * b;
            if (Mathf.Abs(denom) < 0.0001f)
            {
                rayT = 0f;
                lineS = 0f;
                return Vector3.Cross(ray.direction, w0).magnitude;
            }

            rayT = (b * e - c * d) / denom;
            lineS = (a * e - b * d) / denom;
            Vector3 closestRay = ray.origin + ray.direction * rayT;
            Vector3 closestLine = lineOrigin + lineDir * lineS;
            return Vector3.Distance(closestRay, closestLine);
        }

        private bool TryPickSlotTile(Vector2 pointer, Player player, out int slotIndex)
        {
            slotIndex = -1;
            if (Camera.main == null)
                return false;

            Ray ray = Camera.main.ScreenPointToRay(pointer);
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                var handle = hit.collider.GetComponentInParent<HandTileHandle>();
                if (handle == null || !HandTrayController.Instance.OwnsHandle(player, handle))
                    continue;

                slotIndex = handle.SlotIndex;
                return true;
            }

            return false;
        }

        private void BeginDrag(HandTileHandle handle, int axis, Vector2 pointer)
        {
            activeAxis = axis;
            dragging = true;
            dragAxisWorld = gizmoRoot.TransformDirection(AxisDirection(axis)).normalized;
            dragStartLocalPosition = handle.transform.localPosition;
            dragStartLocalRotation = handle.transform.localRotation;
            dragStartPointer = pointer;

            if (mode == GizmoMode.Translate)
            {
                dragStartWorldOnAxis = ClosestPointOnAxis(Camera.main.ScreenPointToRay(pointer), handle.transform.position, dragAxisWorld);
            }
            else
            {
                dragStartAngle = SignedAngleOnPlane(pointer, handle.transform.position, dragAxisWorld);
            }
        }

        private void ContinueDrag(HandTileHandle handle, Vector2 pointer)
        {
            if (mode == GizmoMode.Translate)
            {
                Vector3 currentOnAxis = ClosestPointOnAxis(Camera.main.ScreenPointToRay(pointer), handle.transform.position, dragAxisWorld);
                Vector3 deltaWorld = currentOnAxis - dragStartWorldOnAxis;
                float along = Vector3.Dot(deltaWorld, dragAxisWorld);
                Transform trayRoot = handle.transform.parent;
                Vector3 localAxis = trayRoot != null
                    ? trayRoot.InverseTransformDirection(dragAxisWorld).normalized
                    : dragAxisWorld;
                handle.transform.localPosition = dragStartLocalPosition + localAxis * along;
            }
            else
            {
                float angle = SignedAngleOnPlane(pointer, handle.transform.position, dragAxisWorld);
                float delta = (angle - dragStartAngle) * RotateSensitivity;
                Vector3 localAxis = handle.transform.parent != null
                    ? handle.transform.parent.InverseTransformDirection(dragAxisWorld).normalized
                    : AxisDirection(activeAxis);
                handle.transform.localRotation = Quaternion.AngleAxis(delta, localAxis) * dragStartLocalRotation;
            }

            PositionGizmo(handle.transform);
        }

        private void EndDrag(HandTileHandle handle, HandTrayTunerSettings settings)
        {
            dragging = false;
            activeAxis = -1;
            HandTrayController.Instance?.SyncSlotFromTransform(settings.editingPlayer, handle.SlotIndex, handle.transform);
            tuner?.NotifySlotEdited();
        }

        private Vector3 ClosestPointOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDir)
        {
            DistanceRayToLine(ray, axisOrigin, axisDir, out _, out float lineS);
            return axisOrigin + axisDir * lineS;
        }

        private float SignedAngleOnPlane(Vector2 pointer, Vector3 center, Vector3 axisWorld)
        {
            if (Camera.main == null)
                return 0f;

            Vector3 from = Camera.main.transform.right;
            Vector3 to = Camera.main.ScreenPointToRay(pointer).direction;
            from = Vector3.ProjectOnPlane(from, axisWorld).normalized;
            to = Vector3.ProjectOnPlane(to, axisWorld).normalized;
            if (from.sqrMagnitude < 0.0001f || to.sqrMagnitude < 0.0001f)
                return 0f;

            return Vector3.SignedAngle(from, to, axisWorld);
        }
    }

    public sealed class HandTrayGizmoHandle : MonoBehaviour
    {
        public int AxisIndex;
        public bool IsRotate;
    }
}
