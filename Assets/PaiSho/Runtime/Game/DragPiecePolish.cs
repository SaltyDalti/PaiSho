using UnityEngine;
using PaiSho.Board;

namespace PaiSho.Game
{
    /// <summary>Lift, continuous spin, and wobble while dragging pieces.</summary>
    public class DragPiecePolish : MonoBehaviour
    {
        private const float LiftScale = 1.1f;
        private const float ExtraLift = 0.12f;
        private const float TiltDegrees = 8f;
        private const float SpinDegreesPerSecond = 110f;
        private const float WobbleDegrees = 5f;

        private Transform pieceTransform;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Vector3 lastPosition;
        private PieceShadowFollower shadowFollower;
        private PieceStateAnimator lifeAnimator;
        private float spinYaw;
        private float wobblePhase;
        private bool initialized;

        public void Attach(Transform piece, BoardLayout boardLayout)
        {
            pieceTransform = piece;
            baseScale = piece.localScale;
            baseRotation = piece.rotation;
            spinYaw = piece.eulerAngles.y;
            lastPosition = piece.position;
            wobblePhase = Random.Range(0f, Mathf.PI * 2f);
            lifeAnimator = piece.GetComponent<PieceStateAnimator>();
            lifeAnimator?.Suspend();
            shadowFollower = PieceShadowFollower.Attach(piece, boardLayout);
            initialized = true;
        }

        public void Detach(bool resumeLife = true)
        {
            if (pieceTransform != null)
            {
                pieceTransform.localScale = baseScale;
                // Keep the live yaw the player released with; only clear tilt/wobble.
                pieceTransform.rotation = Quaternion.Euler(0f, spinYaw, 0f);
            }

            shadowFollower?.Detach();
            shadowFollower = null;

            if (resumeLife)
                lifeAnimator?.Resume();
            lifeAnimator = null;
            initialized = false;
            pieceTransform = null;
        }

        /// <summary>Yaw after drag polish (flat on board).</summary>
        public float GetLiveYawDegrees() => spinYaw;

        public void UpdateDrag(Vector3 targetPosition, int? hoverCoordinate)
        {
            if (!initialized || pieceTransform == null)
                return;

            Vector3 delta = targetPosition - lastPosition;
            lastPosition = targetPosition;

            // Explicit lift so board drags read clearly (scale alone was too subtle).
            Vector3 lifted = targetPosition + Vector3.up * ExtraLift;
            pieceTransform.position = Vector3.Lerp(
                pieceTransform.position,
                lifted,
                1f - Mathf.Exp(-18f * Time.deltaTime));

            pieceTransform.localScale = Vector3.Lerp(
                pieceTransform.localScale,
                baseScale * LiftScale,
                1f - Mathf.Exp(-14f * Time.deltaTime));

            // Reduce Motion: keep the lift/scale lift but skip the continuous spin and wobble.
            if (GameSession.ReduceMotion)
            {
                pieceTransform.rotation = Quaternion.Euler(0f, spinYaw, 0f);
                return;
            }

            spinYaw += SpinDegreesPerSecond * Time.deltaTime;
            wobblePhase += Time.deltaTime * 9f;
            float wobble = Mathf.Sin(wobblePhase) * WobbleDegrees;

            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            float tiltX = 0f;
            float tiltZ = wobble;
            if (flatDelta.sqrMagnitude > 0.00001f)
            {
                flatDelta.Normalize();
                tiltX = flatDelta.z * TiltDegrees;
                tiltZ += -flatDelta.x * TiltDegrees;
            }

            pieceTransform.rotation = Quaternion.Euler(tiltX, spinYaw, tiltZ);
        }
    }
}
